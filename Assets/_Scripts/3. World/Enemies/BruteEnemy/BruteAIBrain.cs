using UnityEngine;
using Foundation;
using System.Collections.Generic;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(BlackboardController))]
    public sealed class BruteAIBrain : AIBrain, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;
        
        private BruteEnemyStats _bruteStats => _enemyStats as BruteEnemyStats;
        
        // State Flags for per-frame updating
        private bool _isCharging;
        private bool _isThrusting;
        private bool _isStunned;
        
        private float _chargeCooldownTimer;
        private float _currentStunDuration;
        private Vector3 _lastAttackDirection;

        // References to specific mechanics
        [SerializeField] private BruteThrustHitbox _thrustHitbox;
        [SerializeField] private BruteChargeHitbox _chargeHitbox;
        [SerializeField] private ElementType _bruteElement;
        
        private EnemyController _defenseTarget;

        private void OnEnable() => UpdateManager.Instance.Register(this);
        private void OnDisable()
        {
            UpdateManager.Instance.Unregister(this);
        }

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Brute");

            if (_bruteStats == null)
            {
                Debug.LogError($"{name}: Missing or incorrect stats. Expected BruteEnemyStats.");
                return tree;
            }

            var rootSelector = new PrioritySelectorNode("Brute Root");

            // Priority 50: Spawn Gate
            rootSelector.AddChild(new LeafNode("Spawning", 
                new OneShotGateStrategy(BeginSpawning, () => _bruteStats.SpawnDuration), priority: 50));

            // Priority 40: Stunned Sequence
            var stunSequence = new SequenceNode("Stun Sequence", priority: 40);
            stunSequence.AddChild(new LeafNode("IsStunnedCondition", new ConditionNode(() => _isStunned)));
            stunSequence.AddChild(new LeafNode("StunnedAction", new TimedActionStrategy(BeginStun, () => _currentStunDuration)));
            stunSequence.AddChild(new LeafNode("EndStunAction", new ActionNode(EndStun)));
            rootSelector.AddChild(stunSequence);

            // Priority 30: AoE Thrust Attack
            var thrustSequence = new SequenceNode("Thrust Sequence", priority: 30);
            thrustSequence.AddChild(new LeafNode("CanThrustCondition", new ConditionNode(() => IsPlayerInDistance(_bruteStats.AoEAttackRange) && !_isCharging)));
            thrustSequence.AddChild(new LeafNode("ThrustWindup", new TimedActionStrategy(
                onStart: () => 
                {
                    BeginThrustWindup();
            
                    _lastAttackDirection = (GetPlayer().transform.position - transform.position).normalized;
                    _lastAttackDirection.y = 0;
            
                    _thrustHitbox.ShowTelegraph(_bruteStats, _lastAttackDirection);
                }, 
                getDuration: () => _bruteStats.ThrustWindupDuration
            )));
            thrustSequence.AddChild(new LeafNode("Thrusting", new TimedActionStrategy(StartThrust, () => EffectiveAttackSpeed)));
            thrustSequence.AddChild(new LeafNode("ThrustRecompose", new TimedActionStrategy(StopThrust, () => _bruteStats.ThrustRecomposeDuration)));
            rootSelector.AddChild(thrustSequence);

            // Priority 20: Charge Attack
            var chargeSequence = new SequenceNode("Charge Sequence", priority: 20);
            chargeSequence.AddChild(new LeafNode("CanChargeCondition", new ConditionNode(() => IsPlayerInDistance(_bruteStats.ChargeAttackRange) && _chargeCooldownTimer <= 0f)));
            chargeSequence.AddChild(new LeafNode("ChargeWindup", new TimedActionStrategy(BeginChargeWindup, () => _bruteStats.ChargeWindupDuration)));
            chargeSequence.AddChild(new LeafNode("Charging", new TimedActionStrategy(StartCharge, () => 5f))); // 5s timeout failsafe
            rootSelector.AddChild(chargeSequence);

            // Priority 10: Chase / Defend
            rootSelector.AddChild(new LeafNode("Chase", new ActionNode(HandleChaseAndDefend), priority: 10));

            tree.AddChild(rootSelector);
            return tree;
        }

        // --- Distance Helper ---
        private bool IsPlayerInDistance(float range)
        {
            Transform player = GetPlayer();
            if (player == null) return false;
            return Vector3.Distance(transform.position, player.position) <= range;
        }

        // --- State Actions ---
        private void BeginSpawning() => SetState(AIState.Spawning);

        private void BeginStun()
        {
            SetState(AIState.Stunned);
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            
            var health = GetComponent<EnemyHealth>();
            if (health != null) health.DamageMitigationMultiplier = 1.1f;
        }

        private void EndStun()
        {
            _isStunned = false;
            
            var health = GetComponent<EnemyHealth>();
            if (health != null) health.DamageMitigationMultiplier = 1.0f;
        }

        private void BeginThrustWindup()
        {
            SetState(AIState.Attack);
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            RedirectTowardPlayer();
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
                
                if (toPlayer != Vector3.zero)
                {
                    _lastAttackDirection = toPlayer;
                }
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
                    targetPos = Vector3.Lerp(player.position, _defenseTarget.transform.position, _bruteStats.DefenseTargetLineRatio);
                }
            }

            _agent.SetDestination(targetPos);
        }

        // --- IUpdatable Implementation ---
        public void Tick(float dt)
        {
            if (_chargeCooldownTimer > 0) _chargeCooldownTimer -= dt;

            if (_isCharging && _agent != null && _agent.isOnNavMesh)
            {
                float frameSpeed = EffectiveChaseSpeed * _bruteStats.ChargeSpeedMultiplier * dt;
                _agent.Move(_lastAttackDirection * frameSpeed);
            }
            else if (_isThrusting && _thrustHitbox != null)
            {
                _thrustHitbox.UpdateExpansion(dt, EffectiveAttackSpeed);
            }
        }

        // --- Hitbox Hooks ---
        private void StartCharge()
        {
            _isCharging = true;
    
            if (_chargeHitbox != null)
            {
                // Also pass _lastAttackDirection here so the charge hitbox can orient itself!
                _chargeHitbox.Activate(_bruteStats, _bruteElement, _lastAttackDirection, OnChargeInterrupted);
            }
        }

        private void StartThrust()
        {
            _isThrusting = true;
            if (_thrustHitbox != null)
            {
                int damage = Mathf.RoundToInt(EffectiveAttackDamage);
                
                // CRITICAL FIX: Pass the locked direction to the hitbox
                _thrustHitbox.Activate(_bruteStats, damage, _lastAttackDirection);
            }
        }

        private void StopThrust()
        {
            _isThrusting = false;
            if (_thrustHitbox != null)
            {
                _thrustHitbox.Deactivate();
            }
        }

        private void OnChargeInterrupted(float stunDuration)
        {
            _isCharging = false;
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();
            
            _chargeCooldownTimer = _bruteStats.ChargeCooldown;
            _currentStunDuration = stunDuration;
            _isStunned = true; 
            
            _tree?.Reset();
        }
        
        public override void ResetComponent()
        {
            base.ResetComponent();
            _isCharging = false;
            _isThrusting = false;
            _isStunned = false;
            _chargeCooldownTimer = 0f;
            
            if (_chargeHitbox != null) _chargeHitbox.Deactivate();
            if (_thrustHitbox != null) _thrustHitbox.Deactivate();
        }
    }
}