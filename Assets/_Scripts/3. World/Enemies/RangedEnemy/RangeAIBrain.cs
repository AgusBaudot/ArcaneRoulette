using System;
using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace World
{
    public sealed class RangeAIBrain : AIBrain, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;

        [Header("Range Specific")]
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Collider _hurtBox;

        [Header("Chase (uses the Melee pursuit logic, per FDD)")]
        [SerializeField] private PursuitSettings _pursuit = new PursuitSettings();

        [Header("Teleport")]
        [Tooltip("Chance (0-1) that the destination is picked among the farther half of the zones.")]
        [SerializeField, Range(0f, 1f)] private float _farZoneWeight = 0.8f;
        
        private RangeEnemyStats RangeStats => _enemyStats as RangeEnemyStats;
        private EnemyHealth _health;

        private bool _isUncovering = false;
        private bool _isAttacking = false;
        private bool _isBlocking = false;
        private bool _isTeleporting = false;
        private Vector3 _targetPos2;
        private Vector3 _targetPos3;
        private int _playerProjectileLayerMask;
        private int _enemyLayerMask;

        // --- Deferred Firing State ---
        private int _pendingFireIndex = -1;
        private float _spawnDelayTimer = 0f;

        // --- Teleport zone reservation (two enemies must not arrive in the same zone) ---
        private static readonly HashSet<TeleportZone> ReservedZones = new HashSet<TeleportZone>();
        private readonly List<TeleportZone> _zoneBuffer = new List<TeleportZone>();
        private TeleportZone _reservedZone;
        private float _reservationTimer;

        private bool IsCommitted => _isAttacking || _isBlocking || _isTeleporting;

        public event Action<float> OnTeleportInStarted;
        public event Action<float> OnTeleportOutStarted;
        public event Action<float> OnCoverStarted;
        public event Action<float> OnUncoverStarted;
        public event Action<int, float> OnWindupStarted;
        public event Action<int, float> OnFireStarted;
        public event Action<float> OnRecoverStarted;

        public Vector3 CurrentTargetDirection
        {
            get
            {
                Transform player = GetPlayer();
                return player != null ? (player.position - transform.position).normalized : transform.forward;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ReservedZones.Clear();

        protected override void Awake()
        {
            base.Awake();
            _health = GetComponent<EnemyHealth>();
            _playerProjectileLayerMask = LayerMask.GetMask("PlayerProjectile");
            _enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            ReleaseReservedZone();
        }

        public override void ResetComponent()
        {
            base.ResetComponent();
            _isUncovering = false;
            _isAttacking = false;
            _isBlocking = false;
            _isTeleporting = false;
            _pendingFireIndex = -1;
            ReleaseReservedZone();
            
            if (_health != null) _health.DamageMitigationMultiplier = 1.0f;
        }

        // Dedicated frame-perfect tick to handle exact visual sync
        public void Tick(float dt)
        {
            ApplyPerspectiveSpeed();

            if (_reservedZone != null)
            {
                _reservationTimer -= dt;
                if (_reservationTimer <= 0f) ReleaseReservedZone();
            }

            if (_pendingFireIndex >= 0)
            {
                _spawnDelayTimer -= dt;
                if (_spawnDelayTimer <= 0f)
                {
                    ExecuteFire(_pendingFireIndex);
                    _pendingFireIndex = -1;
                }
            }
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

            //Spawn Gate
            root.AddChild(new LeafNode("Spawning", 
                new OneShotGateStrategy(BeginSpawning, () => RangeStats.SpawnDuration), priority: 40));

            //Teleport
            var teleportSeq = new SequenceNode("Teleport", priority: 30);
            teleportSeq.AddChild(new LeafNode("CanTeleport", new ConditionNode(() => !IsCommitted && IsPlayerInSafeRange())));
            teleportSeq.AddChild(new LeafNode("TeleportInAnim", new TimedActionStrategy(BeginTeleportIn, () => RangeStats.TeleportAnimDuration)));
            teleportSeq.AddChild(new LeafNode("ExecuteTeleport", new ActionNode(ExecuteTeleport)));
            teleportSeq.AddChild(new LeafNode("TeleportOutAnim", new TimedActionStrategy(BeginTeleportOut, () => RangeStats.TeleportOutDuration)));
            root.AddChild(teleportSeq);

            //Block
            var blockSeq = new SequenceNode("Block", priority: 20);
            blockSeq.AddChild(new LeafNode("CanBlock", new ConditionNode(() => !IsCommitted && AreProjectilesInSafeRange())));
            blockSeq.AddChild(new LeafNode("Cover", new TimedActionStrategy(BeginCover, () => RangeStats.BlockCoverDuration)));
            blockSeq.AddChild(new LeafNode("HoldBlock", new BlockHoldStrategy(this, () => RangeStats.BlockTimeout, RangeStats.BlockPercentage)));
            blockSeq.AddChild(new LeafNode("Uncover", new TimedActionStrategy(BeginUncover, () => RangeStats.BlockUncoverDuration)));
            blockSeq.AddChild(new LeafNode("EndUncover", new ActionNode(EndUncover)));
            root.AddChild(blockSeq);

            //Attack
            var attackSeq = new SequenceNode("Attack", priority: 10);
            attackSeq.AddChild(new LeafNode("CanAttack", new ConditionNode(() => IsInStableDistance(GetPlayer()) && IsInLos() && !IsCommitted)));
            
            attackSeq.AddChild(new LeafNode("Windup1", new TimedActionStrategy(BeginWindup1, () => GetClampedWindup(RangeStats.Attack1Windup))));
            attackSeq.AddChild(new LeafNode("Fire1", new TimedActionStrategy(BeginFire1, () => GetClampedDuration(RangeStats.Attack1Duration))));
            
            attackSeq.AddChild(new LeafNode("Windup2", new TimedActionStrategy(BeginWindup2, () => GetClampedWindup(RangeStats.Attack2Windup))));
            attackSeq.AddChild(new LeafNode("Fire2", new TimedActionStrategy(BeginFire2, () => GetClampedDuration(RangeStats.Attack2Duration))));
            
            attackSeq.AddChild(new LeafNode("Windup3", new TimedActionStrategy(BeginWindup3, () => GetClampedWindup(RangeStats.Attack3Windup))));
            attackSeq.AddChild(new LeafNode("Fire3", new TimedActionStrategy(BeginFire3, () => GetClampedDuration(RangeStats.Attack3Duration))));
            
            attackSeq.AddChild(new LeafNode("Windup4", new TimedActionStrategy(BeginWindup4, () => GetClampedWindup(RangeStats.Attack4Windup))));
            attackSeq.AddChild(new LeafNode("FireBig", new TimedActionStrategy(BeginFire4, () => GetClampedDuration(RangeStats.Attack4Duration))));
            
            attackSeq.AddChild(new LeafNode("Recover", new TimedActionStrategy(BeginRecover, () => GetClampedDuration(RangeStats.RecoverDuration))));
            attackSeq.AddChild(new LeafNode("EndAttack", new ActionNode(EndAttack)));
            root.AddChild(attackSeq);

            //Chase
            root.AddChild(new LeafNode("Chase", new ActionNode(DoChase), priority: 0));

            tree.AddChild(root);
            return tree;
        }

        // --- Duration Math ---
        private float GetClampedWindup(float baseWindup) => Mathf.Min(baseWindup * EffectiveAttackSpeed, RangeStats.MaxWindupDuration);
        
        // Multiplies the designer's tuned duration by the attack speed multiplier (accounting for ATK SPD debuffs)
        private float GetClampedDuration(float baseDuration) => Mathf.Max(0.01f, baseDuration * EffectiveAttackSpeed);

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

        private void BeginCover()
        {
            _isBlocking = true;
            SetState(AIState.Blocking);
            _agent.isStopped = true;

            OnCoverStarted?.Invoke(RangeStats.BlockCoverDuration);

            if (RangeStats.BlockStartSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = RangeStats.BlockStartSound, WorldPosition = transform.position });
        }

        public void SetDamageMitigation(float multiplier)
        {
            if (_health != null) _health.DamageMitigationMultiplier = multiplier;
        }

        public void OnBlockHit()
        {
            if (_isBlocking && RangeStats.BlockHitSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = RangeStats.BlockHitSound, WorldPosition = transform.position });
        }

        private void BeginUncover()
        {
            _isUncovering = true;
            SetDamageMitigation(1.0f); 

            OnUncoverStarted?.Invoke(RangeStats.BlockUncoverDuration);

            if (RangeStats.BlockEndSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = RangeStats.BlockEndSound, WorldPosition = transform.position });
        }

        private void EndUncover()
        {
            _isUncovering = false;
            _isBlocking = false;
        }

        private void BeginTeleportIn()
        {
            _isTeleporting = true;
            SetState(AIState.Teleporting);
            _agent.isStopped = true;
            _hurtBox.enabled = false;

            OnTeleportInStarted?.Invoke(RangeStats.TeleportAnimDuration);
        }

        private void ExecuteTeleport()
        {
            if (EntityController.ActiveController == null) return;
            TeleportZone[] zones = EntityController.ActiveController.TeleportZones;
            if (zones == null || zones.Length == 0) return;

            Transform player = GetPlayer();
            Vector3 playerPos = player != null ? player.position : transform.position;
            Vector3 oldPos = transform.position;

            ReleaseReservedZone();

            _zoneBuffer.Clear();
            _zoneBuffer.AddRange(zones);
            _zoneBuffer.Sort((a, b) => 
                Vector3.Distance(b.transform.position, playerPos).CompareTo(Vector3.Distance(a.transform.position, playerPos)));

            int count = _zoneBuffer.Count;
            int startIndex;
            if (Random.value <= _farZoneWeight && count > 1)
            {
                int maxIndex = Mathf.Max(1, count / 2);
                startIndex = Random.Range(0, maxIndex);
            }
            else
            {
                startIndex = Random.Range(0, count);
            }

            for (int i = 0; i < count; i++)
            {
                int checkIndex = (startIndex + i) % count;
                TeleportZone zone = _zoneBuffer[checkIndex];

                if (ReservedZones.Contains(zone)) continue;
                if (!IsZoneOccupied(zone))
                {
                    ReserveZone(zone);
                    WarpAndPlaySounds(oldPos, GetZonePoint(zone));
                    return;
                }
            }

            TeleportZone fallback = _zoneBuffer[0];
            ReserveZone(fallback);
            WarpAndPlaySounds(oldPos, GetZonePoint(fallback));
        }

        private bool IsZoneOccupied(TeleportZone zone)
        {
            BoxCollider box = zone.Collider;
            Vector3 half = Vector3.Scale(box.size, zone.transform.lossyScale) * 0.5f;
            half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));
            return Physics.CheckBox(zone.transform.TransformPoint(box.center), half, zone.transform.rotation, _enemyLayerMask);
        }

        private static Vector3 GetZonePoint(TeleportZone zone) =>
            zone.transform.TransformPoint(zone.Collider.center);

        private void ReserveZone(TeleportZone zone)
        {
            _reservedZone = zone;
            _reservationTimer = RangeStats.TeleportOutDuration + 0.25f;
            ReservedZones.Add(zone);
        }

        private void ReleaseReservedZone()
        {
            if (_reservedZone != null)
            {
                ReservedZones.Remove(_reservedZone);
                _reservedZone = null;
            }
        }

        private void WarpAndPlaySounds(Vector3 oldPos, Vector3 newPos)
        {
            if (RangeStats.TeleportDisappearSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = RangeStats.TeleportDisappearSound, WorldPosition = oldPos });

            Vector3 destination = NavMesh.SamplePosition(newPos, out NavMeshHit hit, 3f, NavMesh.AllAreas)
                ? hit.position
                : newPos;
            _agent.Warp(destination);
            
            if (RangeStats.TeleportAppearSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = RangeStats.TeleportAppearSound, WorldPosition = destination });
        }

        private void BeginTeleportOut()
        {
            OnTeleportOutStarted?.Invoke(RangeStats.TeleportOutDuration);
            _isTeleporting = false;
            _hurtBox.enabled = true;
        }

        // --- Attack Sequence (Modified for Deferred Spawning) ---
        private void BeginWindup1()
        {
            _isAttacking = true;
            SetState(AIState.Attack);
            _agent.isStopped = true;
            
            OnWindupStarted?.Invoke(0, GetClampedWindup(RangeStats.Attack1Windup));
        }

        private void BeginWindup2() => OnWindupStarted?.Invoke(1, GetClampedWindup(RangeStats.Attack2Windup));
        private void BeginWindup3() => OnWindupStarted?.Invoke(2, GetClampedWindup(RangeStats.Attack3Windup));
        private void BeginWindup4() => OnWindupStarted?.Invoke(3, GetClampedWindup(RangeStats.Attack4Windup));

        private void BeginFire1()
        {
            float duration = GetClampedDuration(RangeStats.Attack1Duration);
            _pendingFireIndex = 0;
            _spawnDelayTimer = duration * RangeStats.Attack1ReleaseFraction;
            OnFireStarted?.Invoke(0, duration); // Animates immediately
        }

        private void BeginFire2()
        {
            float duration = GetClampedDuration(RangeStats.Attack2Duration);
            _pendingFireIndex = 1;
            _spawnDelayTimer = duration * RangeStats.Attack2ReleaseFraction;
            OnFireStarted?.Invoke(1, duration);
        }

        private void BeginFire3()
        {
            float duration = GetClampedDuration(RangeStats.Attack3Duration);
            _pendingFireIndex = 2;
            _spawnDelayTimer = duration * RangeStats.Attack3ReleaseFraction;
            OnFireStarted?.Invoke(2, duration);
        }

        private void BeginFire4()
        {
            float duration = GetClampedDuration(RangeStats.Attack4Duration);
            _pendingFireIndex = 3;
            _spawnDelayTimer = duration * RangeStats.Attack4ReleaseFraction;
            OnFireStarted?.Invoke(3, duration);
        }

        private void ExecuteFire(int index)
        {
            Transform player = GetPlayer();
            if (player == null) return;

            PlayerController pc = GetPlayerController();
            Vector3 playerVelocity = pc != null ? pc.LogicalVelocity : Vector3.zero;

            if (index == 0) // Fire 1
            {
                Vector3 dir = (player.position - _firePoint.position).normalized;
                SpawnNormalProjectile(dir);
            }
            else if (index == 1) // Fire 2
            {
                _targetPos2 = player.position + (playerVelocity * RangeStats.PredictionTime2);
                Vector3 dir = (_targetPos2 - _firePoint.position).normalized;
                SpawnNormalProjectile(dir);
            }
            else if (index == 2) // Fire 3
            {
                _targetPos3 = player.position + (playerVelocity * RangeStats.PredictionTime3);
                Vector3 dir = (_targetPos3 - _firePoint.position).normalized;
                SpawnNormalProjectile(dir);
            }
            else if (index == 3) // Fire 4
            {
                Vector3 physicalMidpoint = Vector3.Lerp(_targetPos2, _targetPos3, 0.5f);
                Vector3 dir = (physicalMidpoint - _firePoint.position).normalized;

                int damage = Mathf.RoundToInt(EffectiveAttackDamage); 
                var projObj = Helpers.ProjFactory.Spawn<DetonatingEnemyProjectile>(RangeStats.BigProjectilePrefab, _firePoint.position, Quaternion.LookRotation(dir));
                projObj.InitBig(dir, RangeStats.BigProjectileInitialSpeed, damage, gameObject, RangeStats.BigProjectileDrainRate, RangeStats.BigProjectileLifespan);
                
                if (RangeStats.BigProjectileSound != null)
                    EventBus.Publish(new AudioPlayRequest { Event = RangeStats.BigProjectileSound, WorldPosition = _firePoint.position });
            }
        }

        private void SpawnNormalProjectile(Vector3 dir)
        {
            int damage = Mathf.RoundToInt(EffectiveAttackDamage); 
            var proj = Helpers.ProjFactory.Spawn<EnemyProjectile>(RangeStats.NormalProjectilePrefab, _firePoint.position, Quaternion.LookRotation(dir));
            proj.Init(dir, RangeStats.NormalProjectileSpeed, damage, gameObject);
            
            if (RangeStats.NormalProjectileSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = RangeStats.NormalProjectileSound, WorldPosition = _firePoint.position });
        }

        private void BeginRecover()
        {
            OnRecoverStarted?.Invoke(GetClampedDuration(RangeStats.RecoverDuration));
        }

        private void EndAttack()
        {
            _agent.isStopped = false;
            _isAttacking = false;
            _pendingFireIndex = -1; // Clear any delayed shots if forcefully interrupted
        }

        private void DoChase()
        {
            SetState(AIState.Chase);
            Transform player = GetPlayer();
            if (player == null || _agent == null) return;

            ApplyChaseSpeed();

            bool los = IsInLos();
            float holdRange = Mathf.Max(0.5f, _enemyStats.AttackRange - 1f);
            if (los && FlatDistanceTo(player) <= holdRange)
            {
                StopAgent();
                return;
            }

            Vector3 target = los
                ? GetPursuitPoint(player, _pursuit.PredictionWeight, _pursuit.MaxPredictionTime, 0f, 0f)
                : player.position;

            MoveTo(target, 0.3f);
        }
    }

    public class BlockHoldStrategy : IStrategy
    {
        private readonly RangeAIBrain _brain;
        private readonly Func<float> _getTimeout;
        private float _lastProjectileTime;
        private bool _started;
        private float _blockPercentage;

        public BlockHoldStrategy(RangeAIBrain brain, Func<float> getTimeout, float blockPercentage)
        {
            _brain = brain;
            _getTimeout = getTimeout;
            _blockPercentage = blockPercentage;
        }

        public Node.NodeState Process()
        {
            if (!_started)
            {
                _started = true;
                _lastProjectileTime = Time.time;
                _brain.SetDamageMitigation(_blockPercentage);
            }

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

        public void Reset() 
        {
            _started = false;
        }
    }
}