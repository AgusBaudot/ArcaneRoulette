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
        [SerializeField] protected BehaviourTree tree;
        [SerializeField] protected Transform target;
        [SerializeField] protected string _behaviourTreeName;

        [Header("BB Shared Data")]
        [SerializeField] protected List<Transform> _waypoints;
        protected BlackboardKey hasSeenPlayerKey;
        protected IDebuffReadable _debuffs;
        protected bool _wasInRange;

        [Header("Stats")]
        protected EnemyStats _enemyStats;

        // ---- Init Brain ----
        protected virtual void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _los = new LineOfSight();
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(0, 100);
            
            if (target == null)
            {
                target = GameObject.FindGameObjectWithTag("Player").transform;
            }
            _waypoints.Add(target);
        }
        public void InitComponent(EnemyStats stats, Blackboard bb)
        {
            _blackboard = bb;
            _enemyStats = stats;
            _agent.stoppingDistance = _enemyStats.AttackRange - 1f;
            _los.Init(transform, _enemyStats.viewDistance, _enemyStats.obsMask);
            //hasSeenPlayerKey = _blackboard.GetOrRegisterKey("hasSeenPlayer");
            tree = BuildTree();
        }
        public void ResetComponent()
        {
            _debuffs = null;
            tree?.Reset();
        }
        public void Tick()
        {
            tree?.Process();
        }
        protected abstract BehaviourTree BuildTree();

        // ---- Internal Functions ---- 
        protected virtual bool IsInLos()
        {
            if (_blackboard.TryGetValue<bool>(hasSeenPlayerKey, out var seen) && seen)
                return true;

            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;

            bool hasLOS = _los.CheckRange(target) && _los.CheckView(target);
            _blackboard.SetValue(hasSeenPlayerKey, hasLOS);
            return hasLOS;
        }
        protected virtual bool IsInAttackRangeStable()
        {
            float distance = Vector3.Distance(transform.position, target.position);
            bool result;
            if (_wasInRange)
                result = distance <= _enemyStats.ExitAttackRange;
            else
                result = distance <= _enemyStats.AttackRange;
            _wasInRange = result;
            return result;
        } // Change the method for GetIdealRange to make it more accurate and expose the result into the blackboard
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

        //------------ IDebuffReceiver Implementation ------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
        

        // ---- Gizmos ----
        private void OnDrawGizmos()
        {
            Color myColor = Color.red;
            myColor.a = 0.5f;
            Gizmos.color = myColor;
            Gizmos.DrawWireSphere(transform.position, _enemyStats.AttackRange);
        }
    }
}

