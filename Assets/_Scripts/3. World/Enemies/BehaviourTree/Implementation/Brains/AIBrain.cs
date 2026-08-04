using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(BlackboardController))]
    public abstract class AIBrain : MonoBehaviour, IEnemyComponent, IDebuffReceiver
    {
        public NavMeshAgent Agent => _agent;

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

        [Header("Stats")]
        protected EnemyStats _enemyStats;
        protected AIState _currentState;

        private EnemyHealth _enemyHealth;

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

        // ---- Init Brain ----
        protected virtual void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _los = new LineOfSight();
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(0, 100);

            _waypoints ??= new List<Transform>();

            GetPlayer(); // best-effort now; every real call site retries via GetPlayer() too
            if (_player != null)
                _waypoints.Add(_player);

            // AIBrain and EnemyHealth are siblings, not related by inheritance —
            // this listens to EnemyHealth's already-public OnDeath rather than
            // EnemyHealth needing to reach a protected member on a different
            // component. Awake() only ever runs once per pooled instance's
            // lifetime, so a plain += here is safe without an unsub/resub dance.
            _enemyHealth = GetComponent<EnemyHealth>();
            if (_enemyHealth != null)
                _enemyHealth.OnDeath += HandleDeath;
        }

        private void HandleDeath() => SetState(AIState.Death);

        /// <summary>
        /// Single source of truth for the player reference. Retries the tag
        /// lookup every call until it succeeds once, then just returns the
        /// cached Transform. Use this everywhere instead of reading _player
        /// directly — a direct read never retries, and if it's called before
        /// the real Player exists in the scene (pool pre-warming runs during
        /// Awake now, which can beat the Player's own setup), _player stays
        /// null forever with nothing to notice or recover.
        /// </summary>
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
            _currentState = default;

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
            _tree?.Process();
        }
        protected abstract BehaviorTree BuildTree();
        protected bool IsState(AIState state) => _currentState == state;
        protected void SetState(AIState state) => _currentState = state;

        // ---- Internal Functions ---- 
        protected virtual bool IsInLos()
        {
            if (_blackboard.TryGetValue<bool>(hasSeenPlayerKey, out var seen) && seen)
                return true;

            Transform player = GetPlayer();
            if (player == null)
                return false;

            bool hasLOS = _los.CheckRange(player) && _los.CheckView(player);
            _blackboard.SetValue(hasSeenPlayerKey, hasLOS);
            return hasLOS;
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
        } // Change the method for GetIdealRange to make it more accurate and expose the result into the blackboard

        //------------ IDebuffReceiver Implementation ------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            if (_enemyStats == null) return;

            //attack Range
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