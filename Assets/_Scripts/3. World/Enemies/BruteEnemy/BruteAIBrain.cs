using System;
using Foundation;
using Core;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(BlackboardController))]
    public sealed class BruteAIBrain : AIBrain, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;
        
        private BruteEnemyStats BruteStats => _enemyStats as BruteEnemyStats;
        
        public event Action<float> OnThrustWindupStarted;
        public event Action<float> OnThrustStarted;
        public event Action<float> OnChargeWindupStarted;
        public event Action OnChargeStarted;
        public event Action<float> OnChargeEndedNaturally;
        public event Action OnStunLoopStarted;
        public event Action<float> OnStunRecoverStarted;

        public Vector3 CurrentAttackDirection => _lastAttackDirection;
        
        private bool _isCharging;
        private bool _isChargeWindingUp;
        private bool _isThrusting;
        private bool _isStunned;
        private bool _hasSpawned;
        
        private float _chargeCooldownTimer;
        private float _chargeWindupRemaining;
        private float _currentStunDuration;
        private float _chargeStuckTimer;
        private Vector3 _lastAttackDirection;

        [SerializeField] private BruteThrustHitbox _thrustHitbox;
        [SerializeField] private BruteChargeHitbox _chargeHitbox;
        [SerializeField] private ElementType _bruteElement;

        [Header("Chase (uses the Melee pursuit logic, per FDD)")]
        [SerializeField] private PursuitSettings _pursuit = new PursuitSettings();
        [Tooltip("Swarm slots are clamped to this fraction of the AoE range so a slot is always inside thrust range.")]
        [SerializeField, Range(0.3f, 1f)] private float _slotMaxRangeFraction = 0.8f;

        [Header("Charge windup")]
        [Tooltip("The charge direction follows the player during the windup and locks this many seconds before the charge. >= windup duration = lock at windup start (old behaviour).")]
        [SerializeField] private float _chargeLockTime = 0.35f;
        [SerializeField] private float _chargeTrackTurnSpeed = 360f;

        [Header("Defense")]
        [Tooltip("If no Defense Target was assigned with SetDefenseTarget, protect the nearest living Healer.")]
        [SerializeField] private bool _autoAssignDefenseTarget = true;
        [Tooltip("Within this distance of the player-Healer line the Brute counts as 'on the line' and advances on the player.")]
        [SerializeField] private float _guardLineTolerance = 1.5f;
        [Tooltip("Once on the line, it only goes back to guarding after drifting this far off it.")]
        [SerializeField] private float _guardLineLeaveTolerance = 3.5f;

        private EnemyController _defenseTarget;
        private EnemyHealth _defenseHealth;
        private bool _defenseTargetPinned;
        private bool _isOnGuardLine;
        private float _nextDefenseSearchTime;

        private void OnEnable() => UpdateManager.Instance.Register(this);
        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            SwarmManager.ReleaseSlot(gameObject.GetInstanceID());
        }

        private float GetClampedDuration(float baseDuration) => Mathf.Max(0.01f, baseDuration * EffectiveAttackSpeed);

        /// <summary>Explicitly assign the Healer this Brute protects (e.g. from the room/wave spawner). Pass null to clear.</summary>
        public void SetDefenseTarget(EnemyController target)
        {
            _defenseTarget = target;
            _defenseHealth = target != null ? target.GetComponent<EnemyHealth>() : null;
            _defenseTargetPinned = target != null;
            _isOnGuardLine = false;
        }

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Brute");
            if (BruteStats == null) return tree;

            var rootSelector = new PrioritySelectorNode("Brute Root");

            // Priority 50: Spawn Gate
            var spawnSequence = new SequenceNode("Spawn Sequence", priority: 50);
            spawnSequence.AddChild(new LeafNode("CheckSpawn", new ConditionNode(() => !_hasSpawned)));
            spawnSequence.AddChild(new LeafNode("SpawningAnim", new TimedActionStrategy(BeginSpawning, () => BruteStats.SpawnDuration)));
            spawnSequence.AddChild(new LeafNode("SetSpawned", new ActionNode(() => _hasSpawned = true)));
            rootSelector.AddChild(spawnSequence);

            // Priority 40: Stunned Sequence
            var stunSequence = new SequenceNode("Stun Sequence", priority: 40);
            stunSequence.AddChild(new LeafNode("IsStunnedCondition", new ConditionNode(() => _isStunned)));
            stunSequence.AddChild(new LeafNode("StunLoop", new TimedActionStrategy(
                onStart: () => 
                {
                    BeginStunLoop();
                    OnStunLoopStarted?.Invoke();
                }, 
                getDuration: () => _currentStunDuration
            )));
            stunSequence.AddChild(new LeafNode("StunRecover", new TimedActionStrategy(
                onStart: () => 
                {
                    BeginStunRecover();
                    OnStunRecoverStarted?.Invoke(BruteStats.StunRecoverDuration);
                }, 
                getDuration: () => BruteStats.StunRecoverDuration
            )));
            stunSequence.AddChild(new LeafNode("EndStunAction", new ActionNode(EndStun)));
            rootSelector.AddChild(stunSequence);

            // Priority 30: AoE Thrust Attack
            var thrustSequence = new SequenceNode("Thrust Sequence", priority: 30);
            thrustSequence.AddChild(new LeafNode("CanThrustCondition", new ConditionNode(() => IsPlayerInDistance(BruteStats.AoEAttackRange) && !_isCharging)));
            thrustSequence.AddChild(new LeafNode("ThrustWindup", new TimedActionStrategy(
                onStart: () => 
                {
                    BeginThrustWindup();
                    OnThrustWindupStarted?.Invoke(GetClampedDuration(BruteStats.ThrustWindupDuration));
                }, 
                getDuration: () => GetClampedDuration(BruteStats.ThrustWindupDuration)
            )));
            thrustSequence.AddChild(new LeafNode("Thrusting", new TimedActionStrategy(
                onStart: () => StartThrust(GetClampedDuration(BruteStats.ThrustDuration)), 
                getDuration: () => GetClampedDuration(BruteStats.ThrustDuration)
            )));
            thrustSequence.AddChild(new LeafNode("ThrustRecompose", new TimedActionStrategy(StopThrust, () => GetClampedDuration(BruteStats.ThrustRecomposeDuration))));
            rootSelector.AddChild(thrustSequence);

            // Priority 20: Charge Attack
            var chargeSequence = new SequenceNode("Charge Sequence", priority: 20);
            chargeSequence.AddChild(new LeafNode("CanChargeCondition", new ConditionNode(() => IsPlayerInDistance(BruteStats.ChargeAttackRange) && _chargeCooldownTimer <= 0f)));
            chargeSequence.AddChild(new LeafNode("ChargeWindup", new TimedActionStrategy(
                onStart: () => 
                {
                    BeginChargeWindup();
                    OnChargeWindupStarted?.Invoke(GetClampedDuration(BruteStats.ChargeWindupDuration));
                }, 
                getDuration: () => GetClampedDuration(BruteStats.ChargeWindupDuration)
            )));
            chargeSequence.AddChild(new LeafNode("Charging", new TimedActionStrategy(StartCharge, () => 5f)));
            
            chargeSequence.AddChild(new LeafNode("EndChargeAnim", new TimedActionStrategy(
                onStart: () => 
                {
                    EndCharge();
                    OnChargeEndedNaturally?.Invoke(GetClampedDuration(BruteStats.ChargeMissRecoverDuration));
                },
                getDuration: () => GetClampedDuration(BruteStats.ChargeMissRecoverDuration)
            )));
            
            rootSelector.AddChild(chargeSequence);

            // Priority 10: Chase / Defend
            rootSelector.AddChild(new LeafNode("Chase", new ActionNode(HandleChaseAndDefend), priority: 10));

            tree.AddChild(rootSelector);
            return tree;
        }

        private bool IsPlayerInDistance(float range)
        {
            Transform player = GetPlayer();
            return player != null && Vector3.Distance(transform.position, player.position) <= range;
        }

        private void BeginSpawning() => SetState(AIState.Spawning);

        private void BeginStunLoop()
        {
            SetState(AIState.Stunned);
            _isChargeWindingUp = false;
            StopAgent();
            
            if (_enemyHealth != null) _enemyHealth.DamageMitigationMultiplier = 1.1f;
        }

        private void BeginStunRecover()
        {
            if (_enemyHealth != null) _enemyHealth.DamageMitigationMultiplier = 1.0f;
        }

        private void EndStun() => _isStunned = false;

        private void BeginThrustWindup()
        {
            SetState(AIState.Attack);
            _isChargeWindingUp = false;
            StopAgent();
            RedirectTowardPlayer();
            _thrustHitbox.ShowTelegraph(BruteStats, _lastAttackDirection);
        }

        private void BeginChargeWindup()
        {
            SetState(AIState.Attack);
            StopAgent();
            RedirectTowardPlayer();

            _chargeWindupRemaining = GetClampedDuration(BruteStats.ChargeWindupDuration);
            _isChargeWindingUp = true;
        }

        private void RedirectTowardPlayer()
        {
            _lastAttackDirection = FlatDirectionToPlayer(_lastAttackDirection == Vector3.zero ? transform.forward : _lastAttackDirection);
        }

        private void TrackPlayerDuringChargeWindup(float dt)
        {
            Vector3 toPlayer = FlatDirectionToPlayer(_lastAttackDirection);
            float maxRadians = _chargeTrackTurnSpeed * Mathf.Deg2Rad * dt;
            _lastAttackDirection = Vector3.RotateTowards(_lastAttackDirection, toPlayer, maxRadians, 0f).normalized;
        }

        // ---------------- Chase / Defend ----------------
        private void HandleChaseAndDefend()
        {
            SetState(AIState.Chase);
            Transform player = GetPlayer();
            if (player == null || _agent == null || !_agent.isOnNavMesh) return;

            ApplyChaseSpeed();
            UpdateDefenseTarget();

            if (_defenseTarget != null)
            {
                Vector3 healerPos = _defenseTarget.transform.position;
                float distToLine = DistanceToSegmentXZ(transform.position, player.position, healerPos);

                if (_isOnGuardLine)
                {
                    if (distToLine > _guardLineLeaveTolerance) _isOnGuardLine = false;
                }
                else if (distToLine <= _guardLineTolerance)
                {
                    _isOnGuardLine = true;
                }

                if (!_isOnGuardLine)
                {
                    Vector3 guardPoint = Vector3.Lerp(player.position, healerPos, BruteStats.DefenseTargetLineRatio);
                    MoveTo(guardPoint, 0.3f);
                    return;
                }
            }

            Vector3 target = player.position;
            if (IsInLos())
            {
                target = GetPursuitPoint(player,
                    _pursuit.PredictionWeight,
                    _pursuit.MaxPredictionTime,
                    _pursuit.FormationRadius,
                    BruteStats.AoEAttackRange * _slotMaxRangeFraction);
            }
            MoveTo(target, 0.3f);
        }

        private void UpdateDefenseTarget()
        {
            bool alive = _defenseTarget != null && _defenseTarget.isActiveAndEnabled
                         && _defenseHealth != null && _defenseHealth.CurrentHp > 0f;
            if (alive) return;

            _defenseTarget = null;
            _defenseHealth = null;
            _defenseTargetPinned = false;
            _isOnGuardLine = false;

            if (!_autoAssignDefenseTarget || Time.time < _nextDefenseSearchTime) return;
            _nextDefenseSearchTime = Time.time + 0.5f;

            var healers = HealerAIBrain.Active;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < healers.Count; i++)
            {
                HealerAIBrain healer = healers[i];
                if (healer == null || !healer.isActiveAndEnabled || healer.Controller == null) continue;
                EnemyHealth hp = healer.Health;
                if (hp == null || hp.CurrentHp <= 0f) continue;

                float sqr = (healer.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    _defenseTarget = healer.Controller;
                    _defenseHealth = hp;
                }
            }
        }

        private static float DistanceToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 ab = new Vector2(b.x, b.z) - aa;
            float lenSqr = ab.sqrMagnitude;
            float t = lenSqr > 0.0001f ? Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / lenSqr) : 0f;
            return Vector2.Distance(pp, aa + ab * t);
        }

        // ---------------- Per-frame ----------------
        public void Tick(float dt)
        {
            ApplyPerspectiveSpeed();

            if (_chargeCooldownTimer > 0) _chargeCooldownTimer -= dt;

            if (_isChargeWindingUp)
            {
                _chargeWindupRemaining -= dt;
                if (_chargeWindupRemaining > _chargeLockTime)
                    TrackPlayerDuringChargeWindup(dt);
            }

            if (_isCharging && _agent != null && _agent.isOnNavMesh)
            {
                float frameSpeed = EffectiveChaseSpeed * BruteStats.ChargeSpeedMultiplier * dt;
                Vector3 startPos = transform.position;
                _agent.Move(_lastAttackDirection * frameSpeed);
        
                if (Vector3.Distance(startPos, transform.position) < frameSpeed * 0.1f)
                {
                    _chargeStuckTimer += dt;
                    if (_chargeStuckTimer > 0.05f) OnChargeInterrupted(BruteStats.StunDurationImpact);
                }
                else
                {
                    _chargeStuckTimer = 0f;
                }
            }
            else if (_isThrusting && _thrustHitbox != null)
            {
                _thrustHitbox.UpdateExpansion(dt, EffectiveAttackSpeed);
            }
        }

        private void StartCharge()
        {
            _isChargeWindingUp = false;
            _isCharging = true;
            _chargeStuckTimer = 0f;
            
            if (BruteStats.ChargeSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = BruteStats.ChargeSound, WorldPosition = transform.position });
    
            if (_chargeHitbox != null)
                _chargeHitbox.Activate(BruteStats, _bruteElement, _lastAttackDirection, OnChargeInterrupted);
            
            OnChargeStarted?.Invoke();
        }

        private void StartThrust(float duration)
        {
            _isThrusting = true;
            
            if (BruteStats.ThrustSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = BruteStats.ThrustSound, WorldPosition = transform.position });
            
            if (_thrustHitbox != null)
            {
                int damage = Mathf.RoundToInt(EffectiveAttackDamage);
                _thrustHitbox.Activate(BruteStats, damage, _lastAttackDirection);
            }
            
            OnThrustStarted?.Invoke(duration);
        }

        private void StopThrust()
        {
            _isThrusting = false;
            if (_thrustHitbox != null) _thrustHitbox.Deactivate();
        }

        private void OnChargeInterrupted(float stunDuration)
        {
            _isCharging = false;
            _isChargeWindingUp = false;
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();

            if (BruteStats.ChargeCrashVFX != null)
            {
                Vector3 spawnPos = transform.position + (_lastAttackDirection * 0.5f);
                spawnPos.y = 2f;
                Helpers.ProjFactory.Spawn<PooledVFX>(BruteStats.ChargeCrashVFX, spawnPos, Quaternion.LookRotation(-_lastAttackDirection));
            }
            
            _chargeCooldownTimer = BruteStats.ChargeCooldown;
            _currentStunDuration = stunDuration;
            _isStunned = true; 
            
            _tree?.Reset(); 
        }
        
        private void EndCharge()
        {
            _isCharging = false;
            StopAgent();
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();
            _chargeCooldownTimer = BruteStats.ChargeCooldown;
        }
        
        public override void ResetComponent()
        {
            base.ResetComponent();
            _isCharging = false;
            _isChargeWindingUp = false;
            _isThrusting = false;
            _isStunned = false;
            _hasSpawned = false; 
            _chargeCooldownTimer = 0f;
            _isOnGuardLine = false;
            _nextDefenseSearchTime = 0f;
            if (!_defenseTargetPinned) { _defenseTarget = null; _defenseHealth = null; }
            
            SwarmManager.ReleaseSlot(gameObject.GetInstanceID());
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();
            if (_thrustHitbox != null) _thrustHitbox.Deactivate();
        }
    }
}