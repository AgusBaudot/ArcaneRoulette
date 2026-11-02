using System;
using Foundation;
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
        private bool _isThrusting;
        private bool _isStunned;
        private bool _hasSpawned;
        
        private float _chargeCooldownTimer;
        private float _currentStunDuration;
        private float _chargeStuckTimer;
        private Vector3 _lastAttackDirection;

        [SerializeField] private BruteThrustHitbox _thrustHitbox;
        [SerializeField] private BruteChargeHitbox _chargeHitbox;
        [SerializeField] private ElementType _bruteElement;
        
        private EnemyController _defenseTarget;

        private void OnEnable() => UpdateManager.Instance.Register(this);
        private void OnDisable() => UpdateManager.Instance?.Unregister(this);

        private float GetClampedDuration(float baseDuration) => Mathf.Max(0.01f, baseDuration * EffectiveAttackSpeed);

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Brute");
            if (BruteStats == null) return tree;

            var rootSelector = new PrioritySelectorNode("Brute Root");

            // Priority 50: Spawn Gate
            var spawnSequence = new SequenceNode("Spawn Sequence", priority: 50);
            spawnSequence.AddChild(new LeafNode("CheckSpawn", new ConditionNode(() => !_hasSpawned)));
            // Changed from OneShotGateStrategy to TimedActionStrategy so it succeeds and allows the next node to run
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
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            
            var health = GetComponent<EnemyHealth>();
            if (health != null) health.DamageMitigationMultiplier = 1.1f;
        }

        private void BeginStunRecover()
        {
            var health = GetComponent<EnemyHealth>();
            if (health != null) health.DamageMitigationMultiplier = 1.0f;
        }

        private void EndStun() => _isStunned = false;

        private void BeginThrustWindup()
        {
            SetState(AIState.Attack);
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            RedirectTowardPlayer();
            _thrustHitbox.ShowTelegraph(BruteStats, _lastAttackDirection);
        }

        private void BeginChargeWindup()
        {
            SetState(AIState.Attack);
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            RedirectTowardPlayer();
        }

        private void RedirectTowardPlayer()
        {
            Transform player = GetPlayer();
            if (player != null)
            {
                Vector3 toPlayer = (player.position - transform.position).normalized;
                toPlayer.y = 0;
                if (toPlayer != Vector3.zero) _lastAttackDirection = toPlayer;
            }
        }

        private void HandleChaseAndDefend()
        {
            SetState(AIState.Chase);
            Transform player = GetPlayer();
            if (player == null || _agent == null || !_agent.isOnNavMesh) return;

            _agent.isStopped = false;
            _agent.speed = EffectiveChaseSpeed;

            Vector3 targetPos = player.position;
            if (_defenseTarget != null)
            {
                var targetHealth = _defenseTarget.GetComponent<EnemyHealth>();
                if (targetHealth != null && targetHealth.CurrentHp > 0f)
                {
                    targetPos = Vector3.Lerp(player.position, _defenseTarget.transform.position, BruteStats.DefenseTargetLineRatio);
                }
            }
            _agent.SetDestination(targetPos);
        }

        public void Tick(float dt)
        {
            if (_chargeCooldownTimer > 0) _chargeCooldownTimer -= dt;

            if (_isCharging && _agent != null && _agent.isOnNavMesh)
            {
                float frameSpeed = EffectiveChaseSpeed * BruteStats.ChargeSpeedMultiplier * dt;
                Vector3 startPos = transform.position;
                _agent.Move(_lastAttackDirection * frameSpeed);
        
                if (Vector3.Distance(startPos, transform.position) < frameSpeed * 0.1f)
                {
                    _chargeStuckTimer += dt;
                    // Trigger fast stun immediately if NavMesh halts the charge
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
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();
            
            _chargeCooldownTimer = BruteStats.ChargeCooldown;
            _currentStunDuration = stunDuration;
            _isStunned = true; 
            
            _tree?.Reset(); 
        }
        
        private void EndCharge()
        {
            _isCharging = false;
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();
            _chargeCooldownTimer = BruteStats.ChargeCooldown;
        }
        
        public override void ResetComponent()
        {
            base.ResetComponent();
            _isCharging = false;
            _isThrusting = false;
            _isStunned = false;
            _hasSpawned = false; 
            _chargeCooldownTimer = 0f;
            
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();
            if (_thrustHitbox != null) _thrustHitbox.Deactivate();
        }
    }
}