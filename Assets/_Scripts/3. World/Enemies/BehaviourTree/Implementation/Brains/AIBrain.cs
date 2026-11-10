using System;
using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace World
{
    /// <summary>Melee-style pursuit tunables for brains whose stats asset has none of its own (Range, Brute, Healer).</summary>
    [Serializable]
    public class PursuitSettings
    {
        [Range(0f, 1f)] public float PredictionWeight = 0.5f;
        public float MaxPredictionTime = 1.5f;
        public float FormationRadius = 3f;
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(BlackboardController))]
    public abstract class AIBrain : MonoBehaviour, IEnemyComponent, IDebuffReceiver
    {
        public event Action<AIState> OnStateChanged;
        
        public NavMeshAgent Agent => _agent;
        public EnemyHealth Health => _enemyHealth;

        /// <summary>True while the tree is mid-action (a gate or sequence returned Running). The enemy manager then ticks every frame.</summary>
        public bool NeedsFastTick => _lastTreeResult == Node.NodeState.Running;

        [Header("Components Reference")]
        [SerializeField] protected Animator _animator;
        [SerializeField] protected NavMeshAgent _agent;
        protected LineOfSight _los;

        [Header("Basic AI Data")]
        [SerializeField] protected Blackboard _blackboard;
        [SerializeField] protected BehaviorTree _tree;
        [SerializeField] protected Transform _player;
        [SerializeField] protected string _behaviourTreeName;

        [Header("BB Shared Data")]
        [SerializeField] protected List<Transform> _waypoints;
        protected BlackboardKey hasSeenPlayerKey;
        protected IDebuffReadable _debuffs;
        protected bool _wasInRange;

        [Header("Movement Tuning")]
        [Tooltip("Agent acceleration = chase speed x this. 10 means full speed in ~0.1s.")]
        [SerializeField] private float _accelerationPerSpeed = 10f;
        [SerializeField] private bool _disableAutoBraking = true;
        [SerializeField] private ObstacleAvoidanceType _avoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        [Tooltip("Scale speed by direction with the player's vertical multiplier, so on-screen speed matches on X and Z. Off = old behaviour.")]
        [SerializeField] private bool _perspectiveSpeed = false;
        [Tooltip("Minimum seconds between two path requests.")]
        [SerializeField] private float _minRepathInterval = 0.15f;
        [Tooltip("Even if the target barely moved, repath after this many seconds.")]
        [SerializeField] private float _maxRepathInterval = 0.6f;
        [Tooltip("Repath early when the target moved at least this far.")]
        [SerializeField] private float _repathDistance = 0.75f;

        [Header("Stats")]
        protected EnemyStats _enemyStats;
        protected AIState _currentState;

        protected EnemyHealth _enemyHealth;

        private PlayerController _playerController;
        private KnockbackHandler _knockback;
        private Node.NodeState _lastTreeResult = Node.NodeState.Failure;
        private int _losFrame = -1;
        private bool _losCached;
        private float _lastMoveTime = -999f;
        private Vector3 _lastMoveTarget;
        private float _baseMoveSpeed;

        private static readonly float[] FleeAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };

        protected float EffectiveAttackSpeed
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.AttackSpeed))
                {
                    return _enemyStats.AttackSpeed * Mathf.Max(0f, 1f + _debuffs.GetDebuffStrength(DebuffType.AttackSpeed));
                }
                return _enemyStats.AttackSpeed;
            }
        }
        protected float EffectiveAttackDamage
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.ATK))
                {
                    return _enemyStats.AttackDamage * Mathf.Max(0f, 1f - _debuffs.GetDebuffStrength(DebuffType.ATK));
                }
                return _enemyStats.AttackDamage;
            }
        }
        protected float EffectiveChaseSpeed
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.Speed))
                {
                    return _enemyStats.ChaseSpeed * Mathf.Max(0f, 1f - _debuffs.GetDebuffStrength(DebuffType.Speed));
                }
                return _enemyStats.ChaseSpeed;
            }
        }

        private float VerticalMultiplier => Helpers.PlayerStats != null ? Helpers.PlayerStats.VerticalSpeedMultiplier : 1f;
        protected bool IsKnockedBack => _knockback != null && _knockback.IsKnockedBack;

        protected virtual void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _knockback = GetComponent<KnockbackHandler>();
            _los = new LineOfSight();
            _agent.updateRotation = false;
            _agent.avoidancePriority = Random.Range(0, 100);
            _agent.obstacleAvoidanceType = _avoidanceType;
            if (_disableAutoBraking) _agent.autoBraking = false;

            _waypoints ??= new List<Transform>();

            GetPlayer();
            if (_player != null)
                _waypoints.Add(_player);

            _enemyHealth = GetComponent<EnemyHealth>();
            if (_enemyHealth != null)
                _enemyHealth.OnDeath += HandleDeath;
        }

        private static bool IsWeaponHitbox(Collider col) =>
            col.GetComponent<MeleeWeaponHitbox>() != null ||
            col.GetComponent<BruteChargeHitbox>() != null ||
            col.GetComponent<BruteThrustHitbox>() != null;

        protected void SetCollidersEnabled(bool isEnabled)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (isEnabled && IsWeaponHitbox(col)) continue;
                col.enabled = isEnabled;
            }
        }

        private void HandleDeath()
        {
            SetState(AIState.Death);
            _lastTreeResult = Node.NodeState.Failure;

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }

            SetCollidersEnabled(false);
        }

        protected Transform GetPlayer()
        {
            if (_player == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    _player = playerGO.transform;
            }
            return _player;
        }

        protected PlayerController GetPlayerController()
        {
            if (_playerController == null)
            {
                Transform p = GetPlayer();
                if (p != null) _playerController = p.GetComponentInParent<PlayerController>();
            }
            return _playerController;
        }

        protected Vector3 GetPlayerWorldVelocity()
        {
            PlayerController pc = GetPlayerController();
            if (pc == null) return Vector3.zero;
            Vector3 v = pc.LogicalVelocity;
            v.z *= VerticalMultiplier;
            return v;
        }

        public void InitComponent(EnemyStats stats, Blackboard bb)
        {
            _blackboard = bb;
            _enemyStats = stats;
            _agent.stoppingDistance = _enemyStats.AttackRange - 1f;
            _los.Init(transform, _enemyStats.ViewDistance, _enemyStats.ObsMask);
            
            hasSeenPlayerKey = _blackboard.GetOrRegisterKey("hasSeenPlayer"); 
            
            _tree = BuildTree();
        }
        
        public virtual void ResetComponent()
        {
            _debuffs = null;
            _tree?.Reset();
            _wasInRange = false;
            _lastTreeResult = Node.NodeState.Failure;
            _losFrame = -1;
            _lastMoveTime = -999f;
            _baseMoveSpeed = 0f;
            
            _currentState = (AIState)(-1); 

            SetCollidersEnabled(false);

            if (_animator != null)
            {
                _animator.Rebind();
                _animator.Update(0f); 
            }

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.isStopped = false; 
            }
        }
        
        public void Tick()
        {
            if (IsState(AIState.Death))
            {
                _lastTreeResult = Node.NodeState.Failure;
                return;
            }
            
            _lastTreeResult = _tree != null ? _tree.Process() : Node.NodeState.Failure;
        }
        protected abstract BehaviorTree BuildTree();
        protected bool IsState(AIState state) => _currentState == state;

        protected void SetState(AIState newState)
        {
            if (_currentState == newState)
                return;
            
            AIState previousState = _currentState;
            _currentState = newState;

            if (newState == AIState.Spawning || newState == AIState.Death)
            {
                SetCollidersEnabled(false);
            }
            else if (previousState == AIState.Spawning || previousState == (AIState)(-1))
            {
                SetCollidersEnabled(true);
            }

            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Live line of sight (range + obstacles), evaluated at most once per frame.
        /// The blackboard "hasSeenPlayer" key still latches true on first sight.
        /// </summary>
        protected virtual bool IsInLos()
        {
            int frame = Time.frameCount;
            if (_losFrame == frame) return _losCached;
            _losFrame = frame;

            Transform player = GetPlayer();
            bool los = player != null && _los.CheckRange(player) && _los.CheckView(player);
            if (los && _blackboard != null) _blackboard.SetValue(hasSeenPlayerKey, true);

            _losCached = los;
            return los;
        }
        
        protected virtual bool IsInStableDistance(Transform target)
        {
            if (target == null) 
                return false;

            float distance = Vector3.Distance(transform.position, target.position);
            bool result;
            if (_wasInRange)
                result = distance <= _enemyStats.ExitAttackRange;
            else
                result = distance <= _enemyStats.AttackRange;
            _wasInRange = result;
            return result;
        }

        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;

        #region Movement helpers
        protected bool CanDriveAgent() =>
            _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh && !IsKnockedBack;

        protected void StopAgent()
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
                _agent.isStopped = true;
        }

        /// <summary>Sets speed (debuff-aware) and an acceleration that matches it. Call before MoveTo.</summary>
        protected void ApplyChaseSpeed()
        {
            if (_agent == null || !_agent.isActiveAndEnabled) return;
            _baseMoveSpeed = EffectiveChaseSpeed;
            _agent.speed = _baseMoveSpeed;
            _agent.acceleration = Mathf.Max(8f, _baseMoveSpeed * _accelerationPerSpeed);
        }

        /// <summary>Call every frame from a brain's Tick(dt). No-op unless Perspective Speed is enabled.</summary>
        protected void ApplyPerspectiveSpeed()
        {
            if (!_perspectiveSpeed || _baseMoveSpeed <= 0f || _agent == null) return;
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh || _agent.isStopped) return;

            Vector3 v = _agent.desiredVelocity;
            v.y = 0f;
            if (v.sqrMagnitude < 0.01f) return;
            v.Normalize();

            float vm = VerticalMultiplier;
            float factor = Mathf.Sqrt(v.x * v.x + v.z * v.z * vm * vm);
            _agent.speed = _baseMoveSpeed * factor;
            _agent.acceleration = Mathf.Max(8f, _agent.speed * _accelerationPerSpeed);
        }

        /// <summary>
        /// Throttled, asynchronous move order toward a NavMesh-validated point.
        /// Cheap to call every tick: it only issues a new path request when the target moved enough,
        /// the agent has no path, or the max repath interval elapsed. Never blocks on path calculation.
        /// </summary>
        protected bool MoveTo(Vector3 target, float stoppingDistance)
        {
            if (!CanDriveAgent()) return false;

            _agent.isStopped = false;
            _agent.stoppingDistance = stoppingDistance;
            if (_agent.pathPending) return true;

            float now = Time.time;
            float since = now - _lastMoveTime;
            if (since < _minRepathInterval) return true;

            bool targetMoved = (target - _lastMoveTarget).sqrMagnitude >= _repathDistance * _repathDistance;
            if (_agent.hasPath && !targetMoved && since < _maxRepathInterval) return true;

            Vector3 destination = NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas)
                ? hit.position
                : target;

            _lastMoveTime = now;
            _lastMoveTarget = target;
            return _agent.SetDestination(destination);
        }

        /// <summary>
        /// Melee pursuit point: the player's predicted position (blended by predictionWeight) plus,
        /// if formationRadius > 0, a swarm-slot offset (Z scaled by the player's vertical multiplier so
        /// the ring looks circular on screen). maxSlotDistance > 0 clamps the slot offset so a slot can
        /// never lie outside the attack range.
        /// </summary>
        protected Vector3 GetPursuitPoint(Transform player, float predictionWeight, float maxPredictionTime,
                                          float formationRadius, float maxSlotDistance)
        {
            Vector3 playerPos = player.position;
            Vector3 basePoint = playerPos;

            if (predictionWeight > 0f)
            {
                float distance = Vector3.Distance(transform.position, playerPos);
                float predictionTime = Mathf.Clamp(distance / Mathf.Max(0.1f, EffectiveChaseSpeed), 0f, maxPredictionTime);
                Vector3 futurePos = playerPos + GetPlayerWorldVelocity() * predictionTime;
                basePoint = Vector3.Lerp(playerPos, futurePos, predictionWeight);
            }

            if (formationRadius <= 0f) return basePoint;

            Vector3 offset = SwarmManager.GetOrClaimSlot(gameObject.GetInstanceID(), transform.position, playerPos) * formationRadius;
            offset.z *= VerticalMultiplier;
            if (maxSlotDistance > 0f) offset = Vector3.ClampMagnitude(offset, maxSlotDistance);
            return basePoint + offset;
        }

        /// <summary>Picks a NavMesh-valid point away from threatPos (the inverse of pursuit). False if cornered.</summary>
        protected bool TryGetFleePoint(Vector3 threatPos, float fleeDistance, out Vector3 result)
        {
            result = transform.position;
            if (_agent == null || !_agent.isOnNavMesh) return false;

            Vector3 away = transform.position - threatPos;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = transform.forward;
            away.Normalize();

            bool found = false;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < FleeAngles.Length; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(FleeAngles[i], Vector3.up) * away;
                Vector3 candidate = transform.position + dir * fleeDistance;
                if (NavMesh.Raycast(transform.position, candidate, out NavMeshHit edge, NavMesh.AllAreas))
                    candidate = edge.position;

                float score = (candidate - threatPos).sqrMagnitude;
                if (score > bestScore)
                {
                    bestScore = score;
                    result = candidate;
                    found = true;
                }
            }
            return found;
        }

        protected Vector3 FlatDirectionToPlayer(Vector3 fallback)
        {
            Transform p = GetPlayer();
            if (p == null) return fallback;
            Vector3 d = p.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : fallback;
        }

        protected float FlatDistanceTo(Transform t)
        {
            Vector3 d = t.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            if (_enemyStats == null) return;

            Color Color1 = Color.red;
            Color1.a = 0.5f;
            Gizmos.color = Color1;
            Gizmos.DrawWireSphere(transform.position, _enemyStats.AttackRange);

            Color Color2 = Color.blue;
            Color2.a = 0.5f;
            Gizmos.color = Color2;
            Gizmos.DrawWireSphere(transform.position, _enemyStats.ViewDistance);
        }
        #endregion
    }
}