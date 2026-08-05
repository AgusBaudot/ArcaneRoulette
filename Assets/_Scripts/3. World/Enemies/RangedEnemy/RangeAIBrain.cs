using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class RangeAIBrain : AIBrain
    {
        [Header("Range Specific")]
        [SerializeField] private Transform _firePoint;
        
        private RangeEnemyStats RangeStats => _enemyStats as RangeEnemyStats;
        private PlayerController _playerController;
        private EnemyHealth _health;

        // State trackers
        private bool _isUncovering = false;
        
        // Commitment flags to prevent priority hijacking mid-sequence
        private bool _isAttacking = false;
        private bool _isBlocking = false;
        private bool _isTeleporting = false;
        
        private Vector3 _targetPos2;
        private Vector3 _targetPos3;
        
        private int _playerProjectileLayerMask;

        // Helper to check if we are locked into an uninterruptible sequence
        private bool IsCommitted => _isAttacking || _isBlocking || _isTeleporting;

        protected override void Awake()
        {
            base.Awake();
            _health = GetComponent<EnemyHealth>();
            _playerProjectileLayerMask = LayerMask.GetMask("PlayerProjectile");
        }

        public override void ResetComponent()
        {
            base.ResetComponent();
            _isUncovering = false;
            _isAttacking = false;
            _isBlocking = false;
            _isTeleporting = false;
            if (_health != null) _health.DamageMitigationMultiplier = 1.0f;
        }

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Range");
            if (RangeStats == null)
            {
                Debug.LogError($"{name}: Stats asset isn't a RangeEnemyStats.");
                return tree;
            }

            var root = new PrioritySelectorNode("Range Root");

            // 1. Spawn Gate (Highest Priority)
            root.AddChild(new LeafNode("Spawning", 
                new OneShotGateStrategy(BeginSpawning, () => RangeStats.SpawnDuration), priority: 40));

            // 2. Teleport (Priority 30)
            var teleportSeq = new SequenceNode("Teleport", priority: 30);
            teleportSeq.AddChild(new LeafNode("CanTeleport", new ConditionNode(() => IsPlayerInSafeRange() && !IsCommitted)));
            teleportSeq.AddChild(new LeafNode("TeleportAnim", new TimedActionStrategy(BeginTeleport, () => RangeStats.TeleportAnimDuration)));
            teleportSeq.AddChild(new LeafNode("ExecuteTeleport", new ActionNode(ExecuteTeleport)));
            root.AddChild(teleportSeq);

            // 3. Block (Priority 20)
            var blockSeq = new SequenceNode("Block", priority: 20);
            blockSeq.AddChild(new LeafNode("CanBlock", new ConditionNode(() => AreProjectilesInSafeRange() && !IsCommitted)));
            blockSeq.AddChild(new LeafNode("Cover", new TimedActionStrategy(BeginCover, () => RangeStats.BlockCoverDuration)));
            blockSeq.AddChild(new LeafNode("HoldBlock", new BlockHoldStrategy(this, () => RangeStats.BlockTimeout)));
            blockSeq.AddChild(new LeafNode("Uncover", new TimedActionStrategy(BeginUncover, () => RangeStats.BlockUncoverDuration)));
            blockSeq.AddChild(new LeafNode("EndUncover", new ActionNode(EndUncover)));
            root.AddChild(blockSeq);

            // 4. Attack (Priority 10)
            var attackSeq = new SequenceNode("Attack", priority: 10);
            attackSeq.AddChild(new LeafNode("CanAttack", new ConditionNode(() => IsInStableDistance(GetPlayer()) && IsInLos() && !IsCommitted)));
            attackSeq.AddChild(new LeafNode("Windup1", new TimedActionStrategy(BeginAttack, () => GetClampedWindup(RangeStats.Attack1Windup))));
            attackSeq.AddChild(new LeafNode("Fire1", new ActionNode(FireProjectile1)));
            attackSeq.AddChild(new LeafNode("Windup2", new TimedActionStrategy(null, () => GetClampedWindup(RangeStats.Attack2Windup))));
            attackSeq.AddChild(new LeafNode("Fire2", new ActionNode(FireProjectile2)));
            attackSeq.AddChild(new LeafNode("Windup3", new TimedActionStrategy(null, () => GetClampedWindup(RangeStats.Attack3Windup))));
            attackSeq.AddChild(new LeafNode("Fire3", new ActionNode(FireProjectile3)));
            attackSeq.AddChild(new LeafNode("Windup4", new TimedActionStrategy(null, () => GetClampedWindup(RangeStats.Attack4Windup))));
            attackSeq.AddChild(new LeafNode("FireBig", new ActionNode(FireBigProjectile)));
            attackSeq.AddChild(new LeafNode("EndAttack", new ActionNode(EndAttack)));
            root.AddChild(attackSeq);

            // 5. Chase (Priority 0)
            root.AddChild(new LeafNode("Chase", new ActionNode(DoChase), priority: 0));

            tree.AddChild(root);
            return tree;
        }

        // --- Logic & Conditions ---
        private float GetClampedWindup(float baseWindup) => Mathf.Min(baseWindup * EffectiveAttackSpeed, RangeStats.MaxWindupDuration);
        
        private void BeginSpawning() => SetState(AIState.Spawning);
        
        private bool IsPlayerInSafeRange()
        {
            Transform player = GetPlayer();
            return player != null && Vector3.Distance(transform.position, player.position) <= RangeStats.SafeRange;
        }

        public bool AreProjectilesInSafeRange()
        {
            return Physics.CheckSphere(transform.position, RangeStats.SafeRange, _playerProjectileLayerMask);
        }

        // --- Block ---
        private void BeginCover()
        {
            _isBlocking = true;
            SetState(AIState.Blocking); // Ensure AIState.Blocking exists in your enum
            _agent.isStopped = true;
            _animator.SetTrigger("Cover");
        }

        public void SetDamageMitigation(float multiplier)
        {
            if (_health != null) _health.DamageMitigationMultiplier = multiplier;
        }

        private void BeginUncover()
        {
            _isUncovering = true;
            SetDamageMitigation(1.0f); 
            _animator.SetTrigger("Uncover");
        }

        private void EndUncover()
        {
            _isUncovering = false;
            _isBlocking = false;
        }

        // --- Teleport ---
        private void BeginTeleport()
        {
            _isTeleporting = true;
            SetState(AIState.Teleporting); // Ensure AIState.Teleporting exists in your enum
            _agent.isStopped = true;
            _animator.SetTrigger("TeleportStart");
        }

        private void ExecuteTeleport()
        {
            _isTeleporting = false;
            
            if (EntityController.ActiveController == null) return;
            TeleportZone[] zones = EntityController.ActiveController.TeleportZones;
            if (zones == null || zones.Length == 0) return;

            Transform player = GetPlayer();
            Vector3 playerPos = player != null ? player.position : transform.position;

            System.Array.Sort(zones, (a, b) => 
                Vector3.Distance(b.transform.position, playerPos).CompareTo(Vector3.Distance(a.transform.position, playerPos)));

            float distanceWeight = 0.8f;
    
            int startIndex;
            if (Random.value <= distanceWeight && zones.Length > 1)
            {
                int maxIndex = Mathf.Max(1, zones.Length / 2);
                startIndex = Random.Range(0, maxIndex);
            }
            else
            {
                startIndex = Random.Range(0, zones.Length);
            }

            for (int i = 0; i < zones.Length; i++)
            {
                int checkIndex = (startIndex + i) % zones.Length;
                TeleportZone zone = zones[checkIndex];

                if (!Physics.CheckBox(zone.transform.position, zone.Collider.size / 2f, zone.transform.rotation, LayerMask.GetMask("Enemy")))
                {
                    _agent.Warp(zone.transform.position);
                    return;
                }
            }
    
            _agent.Warp(zones[0].transform.position);
        }

        // --- Attack Sequence ---
        private void BeginAttack()
        {
            _isAttacking = true;
            SetState(AIState.Attack);
            _agent.isStopped = true;
            if (_playerController == null) _playerController = GetPlayer()?.GetComponentInParent<PlayerController>();
        }

        private void FireProjectile1()
        {
            Transform player = GetPlayer();
            if (player == null) return;
            Vector3 dir = (player.position - _firePoint.position).normalized;
            SpawnNormalProjectile(dir);
        }

        private void FireProjectile2()
        {
            Transform player = GetPlayer();
            if (player == null) return;
            _targetPos2 = player.position + (_playerController.LogicalVelocity * RangeStats.PredictionTime2);
            Vector3 dir = (_targetPos2 - _firePoint.position).normalized;
            SpawnNormalProjectile(dir);
        }

        private void FireProjectile3()
        {
            Transform player = GetPlayer();
            if (player == null) return;
            _targetPos3 = player.position + (_playerController.LogicalVelocity * RangeStats.PredictionTime3);
            Vector3 dir = (_targetPos3 - _firePoint.position).normalized;
            SpawnNormalProjectile(dir);
        }

        private void FireBigProjectile()
        {
            Vector3 physicalMidpoint = Vector3.Lerp(_targetPos2, _targetPos3, 0.5f);
            Vector3 dir = (physicalMidpoint - _firePoint.position).normalized;

            int damage = Mathf.RoundToInt(EffectiveAttackDamage); 
            
            var projObj = Helpers.ProjFactory.Spawn<DetonatingEnemyProjectile>(RangeStats.BigProjectilePrefab, _firePoint.position, Quaternion.LookRotation(dir));
            projObj.InitBig(dir, RangeStats.BigProjectileInitialSpeed, damage, RangeStats.ElementType, gameObject, RangeStats.BigProjectileDrainRate);
        }

        private void SpawnNormalProjectile(Vector3 dir)
        {
            int damage = Mathf.RoundToInt(EffectiveAttackDamage); 
            
            var proj = Helpers.ProjFactory.Spawn<EnemyProjectile>(RangeStats.NormalProjectilePrefab, _firePoint.position, Quaternion.LookRotation(dir));
            proj.Init(dir, RangeStats.NormalProjectileSpeed, damage, RangeStats.ElementType, gameObject);
        }

        private void EndAttack()
        {
            _agent.isStopped = false;
            _isAttacking = false;
        }

        // --- Chase ---
        private void DoChase()
        {
            SetState(AIState.Chase);
            Transform player = GetPlayer();
            if (player == null || _agent == null) return;

            _agent.isStopped = false;
            _agent.speed = EffectiveChaseSpeed;
            _agent.SetDestination(player.position);
        }
    }

    // Custom Strategy for Holding the Block
    public class BlockHoldStrategy : IStrategy
    {
        private readonly RangeAIBrain _brain;
        private readonly System.Func<float> _getTimeout;
        private float _lastProjectileTime;

        public BlockHoldStrategy(RangeAIBrain brain, System.Func<float> getTimeout)
        {
            _brain = brain;
            _getTimeout = getTimeout;
        }

        public void OnStart()
        {
            _lastProjectileTime = Time.time;
            _brain.SetDamageMitigation(0.5f); 
        }

        public Node.NodeState Process()
        {
            if (_brain.AreProjectilesInSafeRange())
            {
                _lastProjectileTime = Time.time; 
                return Node.NodeState.Running;
            }
            
            if (Time.time - _lastProjectileTime >= _getTimeout())
            {
                return Node.NodeState.Success; 
            }
            return Node.NodeState.Running;
        }
        public void Reset() {}
    }
}