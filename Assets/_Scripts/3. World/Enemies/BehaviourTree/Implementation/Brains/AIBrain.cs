using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World 
{
    [RequireComponent(typeof(LineOfSight))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public abstract class AIBrain : MonoBehaviour ,IDebuffReceiver
    {
        [Header("Components Reference")]
        [SerializeField] protected Animator _animator;
        [SerializeField] protected NavMeshAgent _agent;
        [SerializeField] protected LineOfSight _los;

        [Header("Common AI Data")]
        [SerializeField] protected Blackboard blackboard;
        [SerializeField] protected BehaviourTree tree;
        [SerializeField] protected Transform target;
        [SerializeField] protected string _behaviourTreeName;

        [Header("Extra")]
        [SerializeField] protected List<Transform> _waypoints;
        BlackboardKey hasSeenPlayerKey;

        protected IDebuffReadable _debuffs;
        protected abstract BehaviourTree BuildTree();
        protected virtual void Awake()
        {

            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _los = GetComponent<LineOfSight>();
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(0, 100);

            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;
            _waypoints.Add(target);

            tree = BuildTree();

            Debug.Log(_agent.isOnNavMesh);
        }
        public void Tick() 
        {
            tree?.Process();
        }
        protected virtual bool IsInLos()
        {
            if (blackboard.TryGetValue<bool>(hasSeenPlayerKey, out var seen) && seen)
                return true;

            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;

            bool hasLOS = _los.CheckRange(target) && _los.CheckView(target);
            blackboard.SetValue(hasSeenPlayerKey, hasLOS);
            return hasLOS;
        }

        //------------ IDebuffReceiver Implementation ------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}

