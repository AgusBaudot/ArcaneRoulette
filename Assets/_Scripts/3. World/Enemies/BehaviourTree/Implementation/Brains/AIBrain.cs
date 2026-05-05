using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World 
{
    [RequireComponent(typeof(LineOfSight))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(BlackboardController))]
    public abstract class AIBrain : MonoBehaviour ,IEnemyUpdate ,IPooleable, IDebuffReceiver
    {
        [Header("Components Reference")]
        [SerializeField] protected Animator _animator;
        [SerializeField] protected NavMeshAgent _agent;
        [SerializeField] protected LineOfSight _los;
        [SerializeField] protected BlackboardController _bbController;

        [Header("Basic AI Data")]
        [SerializeField] protected Blackboard blackboard;
        [SerializeField] protected BehaviourTree tree;
        [SerializeField] protected Transform target;
        [SerializeField] protected string _behaviourTreeName;

        [Header("BB Shared Data")]
        [SerializeField] protected List<Transform> _waypoints;
        protected BlackboardKey hasSeenPlayerKey;

        protected IDebuffReadable _debuffs;

        public float interval { get; set; }
        public float timer { get; set; }
        protected abstract BehaviourTree BuildTree();
        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _los = GetComponent<LineOfSight>();
            _bbController = GetComponent<BlackboardController>();

            blackboard = _bbController.GetBlackboard();
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(0, 100);
            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;
            _waypoints.Add(target);

            tree = BuildTree();
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

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            CustomUpdateEnemyManager.Instance.Register(this);
        }

        private void OnEnable()
        {
            CustomUpdateEnemyManager.Instance.Register(this); // SOLO PARA TESTEO
        }

        public void OnDespawn()
        {
            gameObject.SetActive(false);
            //Reset State & Blackboard
            CustomUpdateEnemyManager.Instance?.Unregister(this);
        }
    }
}

