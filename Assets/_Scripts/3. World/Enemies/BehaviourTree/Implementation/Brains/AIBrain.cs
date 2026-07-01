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

            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player").transform;
            }
            _waypoints.Add(_player);
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
        
        public void ResetComponent()
        {
            _debuffs = null;
            _tree?.Reset();
            _wasInRange = false;

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

            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player").transform;

            bool hasLOS = _los.CheckRange(_player) && _los.CheckView(_player);
            _blackboard.SetValue(hasSeenPlayerKey, hasLOS);
            return hasLOS;
        }
        
        protected virtual bool IsInStableDistance(Transform target)
        {
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
                else return false;
            }

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