using System;
using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class HealerAIBrain : AIBrain, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;

        /// <summary>Enabled Healers in the scene (Brutes use it to pick a Defense Target).</summary>
        public static readonly List<HealerAIBrain> Active = new List<HealerAIBrain>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();

        private static readonly Collider[] OverlapBuffer = new Collider[64];
        private static readonly Comparison<EnemyHealth> ByHpPercent = (a, b) =>
            (a.CurrentHp / Mathf.Max(1f, a.MaxHp)).CompareTo(b.CurrentHp / Mathf.Max(1f, b.MaxHp));

        public EnemyController Controller { get; private set; }

        [Header("Healer Movement")]
        [Tooltip("Melee-style prediction used when approaching the player and when computing where to flee from.")]
        [SerializeField] private PursuitSettings _pursuit = new PursuitSettings();
        [Tooltip("While healing it meanders behind its target: max swing angle (degrees) and speed.")]
        [SerializeField] private float _healSwayAngle = 35f;
        [SerializeField] private float _healSwaySpeed = 0.9f;

        [Header("Healing (default action)")]
        [Tooltip("Max allies healed at once. Each one stays locked until fully healed or dead. 1 = strictly one target.")]
        [SerializeField, Range(1, 3)] private int _maxHealTargets = 2;
        [Tooltip("true: the player entering the safe range cuts a heal in progress (the lock is kept). false: escaping only starts when not healing.")]
        [SerializeField] private bool _escapeInterruptsHeal = true;
        [Tooltip("Once escaping, keep fleeing until the player is this much farther than the Healing Range.")]
        [SerializeField] private float _escapeExitMargin = 1.5f;

        private HealerEnemyStats HealerStats => _enemyStats as HealerEnemyStats;

        private float _throwCooldownEndTime;
        private float _healTimer;
        private float _nextRefillTime;
        private bool _isHealingLoop;
        private bool _healingFinished;
        private bool _isEscaping;

        private readonly List<EnemyHealth> _lockedTargets = new List<EnemyHealth>();
        private readonly List<EnemyHealth> _scratchTargets = new List<EnemyHealth>();
        private readonly List<PooledLineVFX> _activeHealBeams = new List<PooledLineVFX>();

        private AudioHandle _healAudioHandle;

        private bool _isPendingThrow = false;
        private float _spawnDelayTimer = 0f;

        public event Action<float> OnHealWindupStarted;
        public event Action<float> OnHealingStarted;
        public event Action<float> OnHealRecoverStarted;
        public event Action<float> OnThrowWindupStarted;
        public event Action<float> OnThrowStarted;

        public Vector3 CurrentTargetDirection
        {
            get
            {
                if (IsState(AIState.Healing) && _lockedTargets.Count > 0 && _lockedTargets[0] != null)
                {
                    return (_lockedTargets[0].transform.position - transform.position).normalized;
                }
                Transform player = GetPlayer();
                return player != null ? (player.position - transform.position).normalized : transform.forward;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Controller = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            Active.Remove(this);
        }

        public override void ResetComponent()
        {
            _isHealingLoop = false;

            base.ResetComponent();
            _throwCooldownEndTime = 0f;
            _lockedTargets.Clear();
            _isPendingThrow = false;
            _healingFinished = false;
            _isEscaping = false;
            ClearHealBeams();
            StopHealAudio();
        }

        public void Tick(float dt)
        {
            ApplyPerspectiveSpeed();

            if (_isPendingThrow)
            {
                _spawnDelayTimer -= dt;
                if (_spawnDelayTimer <= 0f)
                {
                    ExecuteThrow();
                    _isPendingThrow = false;
                }
            }

            if (_agent == null || !IsState(AIState.Healing))
            {
                _isHealingLoop = false;
                if (_activeHealBeams.Count > 0)
                {
                    ClearHealBeams();
                    StopHealAudio();
                }
                return;
            }

            if (!_isHealingLoop || _healingFinished) return;

            TickHealing(dt);
            if (_healingFinished) return;

            UpdateBeamPositions();
            MoveWhileHealing();
        }

        // ---------------- Tree ----------------
        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Healer");
            if (HealerStats == null) return tree;

            var root = new PrioritySelectorNode("Healer Root");

            root.AddChild(new LeafNode("Spawning",
                new OneShotGateStrategy(() => SetState(AIState.Spawning), () => HealerStats.SpawnDuration), priority: 50));

            root.AddChild(BuildThrowingSequence());                                                              // 40
            root.AddChild(new LeafNode("ApproachForThrow", new ConditionNode(TryApproachForThrow), priority: 35));
            root.AddChild(new LeafNode("Escape", new ConditionNode(TryEscape), priority: 30));
            root.AddChild(BuildHealingSequence());                                                               // 20
            root.AddChild(new LeafNode("FindTarget", new ActionNode(DoFindTarget), priority: 0));

            tree.AddChild(root);
            return tree;
        }

        private Node BuildThrowingSequence()
        {
            var seq = new SequenceNode("ThrowingSequence", priority: 40);
            seq.AddChild(new LeafNode("CanThrow", new ConditionNode(() => 
                Time.time >= _throwCooldownEndTime && IsPlayerInThrowingRange())));
            
            seq.AddChild(new LeafNode("ThrowWindup", new TimedActionStrategy(BeginThrowWindup, () => GetClampedDuration(HealerStats.ThrowWindupDuration))));
            seq.AddChild(new LeafNode("ThrowAction", new TimedActionStrategy(BeginThrow, () => GetClampedDuration(HealerStats.ThrowDuration))));
            seq.AddChild(new LeafNode("EndThrow", new ActionNode(EndThrowing)));
            return seq;
        }

        private Node BuildHealingSequence()
        {
            var seq = new SequenceNode("HealingSequence", priority: 20);
            seq.AddChild(new LeafNode("CanHeal", new ConditionNode(HasHealTarget)));
            seq.AddChild(new LeafNode("HealWindup", new TimedActionStrategy(BeginHealWindup, () => GetClampedDuration(HealerStats.HealWindupDuration))));
            seq.AddChild(new LeafNode("HealAction", new HealUntilDoneStrategy(this)));
            seq.AddChild(new LeafNode("HealRecover", new TimedActionStrategy(BeginHealRecover, () => GetClampedDuration(HealerStats.HealRecoverDuration))));
            seq.AddChild(new LeafNode("EndHeal", new ActionNode(EndHealing)));
            return seq;
        }

        private sealed class HealUntilDoneStrategy : IStrategy
        {
            private readonly HealerAIBrain _brain;
            private bool _started;

            public HealUntilDoneStrategy(HealerAIBrain brain) { _brain = brain; }

            public Node.NodeState Process()
            {
                if (!_started)
                {
                    _started = true;
                    _brain.BeginHealing();
                }
                return _brain._healingFinished ? Node.NodeState.Success : Node.NodeState.Running;
            }

            public void Reset()
            {
                _started = false;
                _brain.OnHealSequenceReset();
            }
        }

        private float GetClampedDuration(float baseDuration) => Mathf.Max(0.01f, baseDuration * EffectiveAttackSpeed);

        private Vector3 PredictedPlayerPoint(Transform player) =>
            GetPursuitPoint(player, _pursuit.PredictionWeight, _pursuit.MaxPredictionTime, 0f, 0f);

        // ---------------- Movement leaves ----------------
        
        private bool TryApproachForThrow()
        {
            if (Time.time < _throwCooldownEndTime) return false;

            Transform player = GetPlayer();
            if (player == null || _agent == null) return false;

            SetState(AIState.Chase);
            ApplyChaseSpeed();
            MoveTo(PredictedPlayerPoint(player), Mathf.Max(0.5f, _enemyStats.AttackRange - 1f));
            return true;
        }

        private bool TryEscape()
        {
            Transform player = GetPlayer();
            if (player == null || _agent == null) { _isEscaping = false; return false; }
            if (!_escapeInterruptsHeal && IsState(AIState.Healing)) { _isEscaping = false; return false; }

            float enter = HealerStats.HealingRange;
            float exit = enter + _escapeExitMargin;
            float distance = FlatDistanceTo(player);
            if (distance >= (_isEscaping ? exit : enter)) { _isEscaping = false; return false; }

            if (!TryGetFleePoint(PredictedPlayerPoint(player), enter + 2f, out Vector3 flee)
                || (flee - transform.position).sqrMagnitude < 1f)
            {
                _isEscaping = false;
                return false;
            }

            _isEscaping = true;
            SetState(AIState.Chase);
            ApplyChaseSpeed();
            MoveTo(flee, 0.3f);
            return true;
        }

        private void DoFindTarget()
        {
            SetState(AIState.Chase);
            if (_agent == null) return;

            ApplyChaseSpeed();

            EnemyHealth ally = FindLowestHpAllyGlobal();
            if (ally != null)
                MoveTo(ally.transform.position, Mathf.Max(0.5f, HealerStats.HealingRange - 2f));
            else
                StopAgent();
        }

        private void MoveWhileHealing()
        {
            if (_lockedTargets.Count == 0) return;

            EnemyHealth ally = _lockedTargets[0];
            if (ally == null) return;

            Transform player = GetPlayer();
            Vector3 allyPos = ally.transform.position;

            Vector3 behind = player != null ? allyPos - player.position : -transform.forward;
            behind.y = 0f;
            behind = behind.sqrMagnitude > 0.0001f ? behind.normalized : -transform.forward;

            float sway = Mathf.Sin(Time.time * _healSwaySpeed) * _healSwayAngle;
            Vector3 ideal = allyPos + Quaternion.AngleAxis(sway, Vector3.up) * behind * HealerStats.SpacingOffset;

            ApplyChaseSpeed();
            MoveTo(ideal, 0.2f);
        }

        // ---------------- Healing ----------------
        private bool HasHealTarget()
        {
            PruneLocked();
            if (_lockedTargets.Count > 0) return true;
            return CollectHurtAllies(_scratchTargets, HealerStats.HealingRange, 1, null) > 0;
        }

        private void BeginHealWindup()
        {
            SetState(AIState.Healing);
            _isPendingThrow = false;
            StopAgent();
            OnHealWindupStarted?.Invoke(GetClampedDuration(HealerStats.HealWindupDuration));
        }

        private void BeginHealing()
        {
            _healTimer = 0f;
            _healingFinished = false;
            _nextRefillTime = 0f;

            PruneLocked();
            RefillLocked();

            _isHealingLoop = true;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = false;

            OnHealingStarted?.Invoke(GetClampedDuration(HealerStats.HealingDuration));

            if (_lockedTargets.Count == 0)
            {
                _healingFinished = true;
                return;
            }

            for (int i = 0; i < _lockedTargets.Count; i++) SpawnBeam();

            if (HealerStats.HealBeamLoopSound != null)
            {
                EventBus.Publish(new AudioPlayTrackedRequest
                {
                    Event = HealerStats.HealBeamLoopSound,
                    WorldPosition = transform.position,
                    OnHandleReady = handle => _healAudioHandle = handle
                });
            }
        }

        private void TickHealing(float dt)
        {
            PruneLocked();

            if (_lockedTargets.Count < _maxHealTargets && (_lockedTargets.Count == 0 || Time.time >= _nextRefillTime))
            {
                _nextRefillTime = Time.time + 0.25f;
                RefillLocked();
            }

            if (_lockedTargets.Count == 0)
            {
                _healingFinished = true;
                return;
            }

            _healTimer += dt;
            if (_healTimer >= 2f)
            {
                _healTimer -= 2f;
                float healAmount = EffectiveAttackDamage / 3f;
                for (int i = 0; i < _lockedTargets.Count; i++)
                    _lockedTargets[i].Heal(healAmount);
            }
        }

        // Locked-target upkeep -------------------------------------------------
        private bool IsLockValid(EnemyHealth t) =>
            t != null && t.isActiveAndEnabled && t.CurrentHp > 0f && t.CurrentHp < t.MaxHp;

        private void PruneLocked()
        {
            for (int i = _lockedTargets.Count - 1; i >= 0; i--)
            {
                if (!IsLockValid(_lockedTargets[i])) RemoveLockedAt(i);
            }
        }

        private void RemoveLockedAt(int index)
        {
            _lockedTargets.RemoveAt(index);
            if (index < _activeHealBeams.Count)
            {
                if (_activeHealBeams[index] != null) Helpers.ProjFactory.Despawn(_activeHealBeams[index].gameObject);
                _activeHealBeams.RemoveAt(index);
            }
        }

        private void RefillLocked()
        {
            int needed = _maxHealTargets - _lockedTargets.Count;
            if (needed <= 0) return;

            CollectHurtAllies(_scratchTargets, HealerStats.HealingRange, needed, _lockedTargets);
            for (int i = 0; i < _scratchTargets.Count; i++)
            {
                _lockedTargets.Add(_scratchTargets[i]);
                if (_isHealingLoop) SpawnBeam();
            }
        }

        private void SpawnBeam()
        {
            if (HealerStats.HealBeamPrefab == null) return;
            var beam = Helpers.ProjFactory.Spawn(HealerStats.HealBeamPrefab, transform.position, Quaternion.identity);
            _activeHealBeams.Add(beam);
        }

        private void UpdateBeamPositions()
        {
            for (int i = 0; i < _lockedTargets.Count && i < _activeHealBeams.Count; i++)
            {
                if (_lockedTargets[i] == null || _activeHealBeams[i] == null) continue;
                if (_activeHealBeams[i].TryGetComponent<LineRenderer>(out var lr))
                {
                    lr.SetPosition(0, transform.position + Vector3.up * 2f);
                    lr.SetPosition(1, _lockedTargets[i].transform.position + Vector3.up * 2f);
                }
            }
        }

        private void OnHealSequenceReset()
        {
            if (!_isHealingLoop) return;

            _isHealingLoop = false;
            ClearHealBeams();
            StopHealAudio();
            OnHealRecoverStarted?.Invoke(GetClampedDuration(HealerStats.HealRecoverDuration));
        }

        private void BeginHealRecover()
        {
            _isHealingLoop = false;
            ClearHealBeams();
            StopHealAudio();
            StopAgent();
            OnHealRecoverStarted?.Invoke(GetClampedDuration(HealerStats.HealRecoverDuration));
        }

        private void EndHealing()
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = false;
        }

        private void ClearHealBeams()
        {
            foreach (var beam in _activeHealBeams)
            {
                if (beam != null) Helpers.ProjFactory.Despawn(beam.gameObject);
            }
            _activeHealBeams.Clear();
        }
        
        private void StopHealAudio()
        {
            if (_healAudioHandle != null)
            {
                EventBus.Publish(new AudioStopRequest
                {
                    Handle = _healAudioHandle,
                    FadeOut = true,
                });
                _healAudioHandle = null;
            }
        }

        // ---------------- Throwing ----------------
        private void BeginThrowWindup()
        {
            SetState(AIState.Throwing);
            StopAgent();
            OnThrowWindupStarted?.Invoke(GetClampedDuration(HealerStats.ThrowWindupDuration));
        }

        private void BeginThrow()
        {
            float duration = GetClampedDuration(HealerStats.ThrowDuration);
            _isPendingThrow = true;
            _spawnDelayTimer = duration * HealerStats.BottleReleaseFraction;
            
            OnThrowStarted?.Invoke(duration);
        }

        private void ExecuteThrow()
        {
            Transform player = GetPlayer();
            if (player != null && HealerStats.BottlePrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 1f;
                Vector3 dir = (player.position - spawnPos).normalized;
                
                var bottle = Helpers.ProjFactory.Spawn<BottleProjectile>(HealerStats.BottlePrefab, spawnPos, HealerStats.BottlePrefab.transform.rotation);
                
                bottle.InitEnemyBottle(
                    HealerStats.BottlePrefab, 
                    dir, 
                    HealerStats.BottleThrowSpeed, 
                    Mathf.RoundToInt(EffectiveAttackDamage * 0.5f), 
                    gameObject, 
                    player.position
                );
            }
            
            if (HealerStats.BottleThrowSound != null)
            {
                EventBus.Publish(new AudioPlayRequest
                {
                    Event = HealerStats.BottleThrowSound,
                    WorldPosition = transform.position,
                });
            }
        }

        private void EndThrowing()
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = false;
            _isPendingThrow = false;
            _throwCooldownEndTime = Time.time + HealerStats.ThrowingCooldown;
        }

        // ---------------- Ally queries (allocation-free) ----------------
        private bool IsHealableAlly(EnemyHealth health)
        {
            if (health == null || health == _enemyHealth) return false;
            if (health.CurrentHp <= 0f || health.CurrentHp >= health.MaxHp) return false;
            if (health.GetComponent<HealerAIBrain>() != null) return false;
            return true;
        }

        private int CollectHurtAllies(List<EnemyHealth> output, float radius, int maxCount, List<EnemyHealth> exclude)
        {
            output.Clear();
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, OverlapBuffer, HealerStats.HitLayer);
            for (int i = 0; i < count; i++)
            {
                EnemyHealth health = OverlapBuffer[i].GetComponentInParent<EnemyHealth>();
                if (!IsHealableAlly(health) || output.Contains(health)) continue;
                if (exclude != null && exclude.Contains(health)) continue;
                output.Add(health);
            }

            output.Sort(ByHpPercent);
            if (output.Count > maxCount) output.RemoveRange(maxCount, output.Count - maxCount);
            return output.Count;
        }

        private EnemyHealth FindLowestHpAllyGlobal()
        {
            if (EntityController.ActiveController == null) return null;

            EnemyHealth lowest = null;
            float lowestPct = float.MaxValue;
            int count = Physics.OverlapSphereNonAlloc(transform.position, 50f, OverlapBuffer, HealerStats.HitLayer);
            for (int i = 0; i < count; i++)
            {
                EnemyHealth health = OverlapBuffer[i].GetComponentInParent<EnemyHealth>();
                if (!IsHealableAlly(health)) continue;

                float pct = health.CurrentHp / Mathf.Max(1f, health.MaxHp);
                if (pct < lowestPct)
                {
                    lowestPct = pct;
                    lowest = health;
                }
            }
            return lowest;
        }

        private bool IsPlayerInThrowingRange()
        {
            Transform p = GetPlayer();
            return p != null && Vector3.Distance(transform.position, p.position) <= HealerStats.ThrowingRange;
        }
    }
}