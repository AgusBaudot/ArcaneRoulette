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
        [Header("Components Reference")]
        [SerializeField] protected Animator _animator;
        [SerializeField] protected NavMeshAgent _agent;
        protected LineOfSight _los;
        public NavMeshAgent Agent => _agent;

        [Header("Basic AI Data")]
        [SerializeField] protected Blackboard _blackboard;
        [SerializeField] protected BehaviourTree tree;
        [SerializeField] protected Transform target;
        [SerializeField] protected string _behaviourTreeName;

        [Header("BB Shared Data")]
        [SerializeField] protected List<Transform> _waypoints;
        protected BlackboardKey hasSeenPlayerKey;
        protected IDebuffReadable _debuffs;

        [Header("Stats")]
        protected EnemyStats _enemyStats;

        protected virtual void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _los = new LineOfSight();
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
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

