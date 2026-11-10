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

        /// <summary>Living Healers in the scene (used by Brutes to pick a Defense Target).</summary>
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
        [Tooltip("While healing it meanders behind the ally: max swing angle (degrees) and speed.")]
        [SerializeField] private float _healSwayAngle = 35f;
        [SerializeField] private float _healSwaySpeed = 0.9f;

        private HealerEnemyStats HealerStats => _enemyStats as HealerEnemyStats;

        private float _healCooldownEndTime;
        private float _throwCooldownEndTime;
        private float _healTimer;
        private bool _isHealingLoop;
        private bool _abortHealing;
        private Node _healSequence;
        private List<EnemyHealth> _activeHealTargets = new List<EnemyHealth>();
        private List<EnemyHealth> _scratchTargets = new List<EnemyHealth>();
        private List<PooledLineVFX> _activeHealBeams = new List<PooledLineVFX>();
        
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
                if (IsState(AIState.Healing) && _activeHealTargets.Count > 0 && _activeHealTargets[0] != null)
                {
                    return (_activeHealTargets[0].transform.position - transform.position).normalized;
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
            base.ResetComponent();
            _healCooldownEndTime = 0f;
            _throwCooldownEndTime = 0f;
            _activeHealTargets.Clear();
            _isPendingThrow = false;
            _isHealingLoop = false;
            _abortHealing = false;
            ClearHealBeams();
            StopHealAudio();
        }

        public void Tick(float dt)
        {
            ApplyPerspectiveSpeed();

            if (_abortHealing)
            {
                _abortHealing = false;
                FinishHealingEarly();
            }

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

            for (int i = 0; i < _activeHealTargets.Count; i++)
            {
                if (_activeHealTargets[i] != null && i < _activeHealBeams.Count && _activeHealBeams[i] != null)
                {
                    if (_activeHealBeams[i].TryGetComponent<LineRenderer>(out var lr))
                    {
                        lr.SetPosition(0, transform.position + Vector3.up * 2f);
                        lr.SetPosition(1, _activeHealTargets[i].transform.position + Vector3.up * 2f);
                    }
                }
            }

            MoveWhileHealing();
        }

        private void MoveWhileHealing()
        {
            if (!_isHealingLoop || _activeHealTargets.Count == 0) return;

            EnemyHealth ally = _activeHealTargets[0];
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

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Healer");
            if (HealerStats == null) return tree;

            var root = new PrioritySelectorNode("Healer Root");

            root.AddChild(new LeafNode("Spawning",
                new OneShotGateStrategy(() => SetState(AIState.Spawning), () => HealerStats.SpawnDuration), priority: 50));

            _healSequence = BuildHealingSequence();
            root.AddChild(_healSequence);
            root.AddChild(BuildThrowingSequence());
            root.AddChild(new LeafNode("Chase", new ActionNode(DoChase), priority: 0));

            tree.AddChild(root);
            return tree;
        }

        private Node BuildHealingSequence()
        {
            var seq = new SequenceNode("HealingSequence", priority: 40);
            seq.AddChild(new LeafNode("CanHeal", new ConditionNode(() => 
                Time.time >= _healCooldownEndTime && HasHealTarget())));
            
            seq.AddChild(new LeafNode("HealWindup", new TimedActionStrategy(BeginHealWindup, () => GetClampedDuration(HealerStats.HealWindupDuration))));
            seq.AddChild(new LeafNode("HealAction", new TimedActionStrategy(BeginHealing, () => GetClampedDuration(HealerStats.HealingDuration), OnHealingTick)));
            seq.AddChild(new LeafNode("HealRecover", new TimedActionStrategy(BeginHealRecover, () => GetClampedDuration(HealerStats.HealRecoverDuration))));
            seq.AddChild(new LeafNode("EndHeal", new ActionNode(EndHealing)));
            return seq;
        }

        private Node BuildThrowingSequence()
        {
            var seq = new SequenceNode("ThrowingSequence", priority: 30);
            seq.AddChild(new LeafNode("CanThrow", new ConditionNode(() => 
                Time.time >= _throwCooldownEndTime && IsPlayerInThrowingRange())));
            
            seq.AddChild(new LeafNode("ThrowWindup", new TimedActionStrategy(BeginThrowWindup, () => GetClampedDuration(HealerStats.ThrowWindupDuration))));
            seq.AddChild(new LeafNode("ThrowAction", new TimedActionStrategy(BeginThrow, () => GetClampedDuration(HealerStats.ThrowDuration))));
            seq.AddChild(new LeafNode("EndThrow", new ActionNode(EndThrowing)));
            return seq;
        }
        
        private float GetClampedDuration(float baseDuration) => Mathf.Max(0.01f, baseDuration * EffectiveAttackSpeed);

        private Vector3 PredictedPlayerPoint(Transform player) =>
            GetPursuitPoint(player, _pursuit.PredictionWeight, _pursuit.MaxPredictionTime, 0f, 0f);

        private void DoChase()
        {
            SetState(AIState.Chase);
            if (_agent == null) return;

            Transform player = GetPlayer();
            if (player == null) return;

            ApplyChaseSpeed();

            bool isHealReady = Time.time >= _healCooldownEndTime;
            bool isThrowReady = Time.time >= _throwCooldownEndTime;
            float approachStop = Mathf.Max(0.5f, _enemyStats.AttackRange - 1f);

            if (isThrowReady && !isHealReady)
            {
                MoveTo(PredictedPlayerPoint(player), approachStop);
            }
            else if (isHealReady)
            {
                EnemyHealth lowestHpAlly = FindLowestHpAllyGlobal();
                if (lowestHpAlly != null)
                    MoveTo(lowestHpAlly.transform.position, Mathf.Max(0.5f, HealerStats.HealingRange - 2f));
                else
                    MoveTo(PredictedPlayerPoint(player), approachStop);
            }
            else
            {
                if (FlatDistanceTo(player) < HealerStats.HealingRange)
                {
                    if (TryGetFleePoint(PredictedPlayerPoint(player), HealerStats.HealingRange + 2f, out Vector3 fleePos))
                        MoveTo(fleePos, 0.3f);
                }
                else
                {
                    StopAgent();
                }
            }
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
            CollectHurtAllies(_activeHealTargets, HealerStats.HealingRange, 2);
            _isHealingLoop = true;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = false;
            
            OnHealingStarted?.Invoke(GetClampedDuration(HealerStats.HealingDuration));

            SpawnBeamsForTargets();

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

        private void SpawnBeamsForTargets()
        {
            if (HealerStats.HealBeamPrefab == null) return;
            for (int i = 0; i < _activeHealTargets.Count; i++)
            {
                var beam = Helpers.ProjFactory.Spawn(HealerStats.HealBeamPrefab, transform.position, Quaternion.identity);
                _activeHealBeams.Add(beam);
            }
        }

        private void OnHealingTick(float dt)
        {
            _healTimer += dt;
            if (_healTimer < 2f) return;
            _healTimer -= 2f;

            float healAmount = EffectiveAttackDamage / 3f;
            
            for (int i = _activeHealTargets.Count - 1; i >= 0; i--)
            {
                if (_activeHealTargets[i] == null || _activeHealTargets[i].CurrentHp <= 0f)
                {
                    _activeHealTargets.RemoveAt(i);
                    if (i < _activeHealBeams.Count)
                    {
                        if (_activeHealBeams[i] != null) Helpers.ProjFactory.Despawn(_activeHealBeams[i].gameObject);
                        _activeHealBeams.RemoveAt(i);
                    }
                }
            }
            
            if (_activeHealTargets.Count == 0)
            {
                if (CollectHurtAllies(_activeHealTargets, HealerStats.HealingRange, 2) == 0)
                {
                    _abortHealing = true;
                    return;
                }
                SpawnBeamsForTargets();
            }

            for (int i = 0; i < _activeHealTargets.Count; i++)
            {
                _activeHealTargets[i].Heal(healAmount);
            }
        }

        private void FinishHealingEarly()
        {
            _isHealingLoop = false;
            ClearHealBeams();
            StopHealAudio();
            _healCooldownEndTime = Time.time + HealerStats.HealingCooldown;
            _healSequence?.Reset();
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
            _healCooldownEndTime = Time.time + HealerStats.HealingCooldown;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = false;
        }
        
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

        // ---------------- Ally queries (allocation-free) ----------------
        private bool IsHealableAlly(EnemyHealth health)
        {
            if (health == null || health == _enemyHealth) return false;
            if (health.CurrentHp <= 0f || health.CurrentHp >= health.MaxHp) return false;
            if (health.GetComponent<HealerAIBrain>() != null) return false;
            return true;
        }

        private int CollectHurtAllies(List<EnemyHealth> output, float radius, int maxCount)
        {
            output.Clear();
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, OverlapBuffer, HealerStats.HitLayer);
            for (int i = 0; i < count; i++)
            {
                EnemyHealth health = OverlapBuffer[i].GetComponentInParent<EnemyHealth>();
                if (!IsHealableAlly(health) || output.Contains(health)) continue;
                output.Add(health);
            }

            output.Sort(ByHpPercent);
            if (output.Count > maxCount) output.RemoveRange(maxCount, output.Count - maxCount);
            return output.Count;
        }

        private bool HasHealTarget() => CollectHurtAllies(_scratchTargets, HealerStats.HealingRange, 1) > 0;

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