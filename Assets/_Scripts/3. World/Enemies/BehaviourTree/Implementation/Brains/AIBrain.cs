using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World 
{
    [RequireComponent(typeof(LineOfSight))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public abstract class AIBrain : MonoBehaviour, IEnemyUpdate ,IDebuffReceiver
    {
        // ---- Custom Update Enemy Manager ----
        public float interval { get; set; }
        public float timer { get; set; }


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
        protected virtual bool IsInLos()
        {
            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;
            if (_los == null || target == null) return false;
            return _los.CheckRange(target) && _los.CheckView(target);
        }

        bool LineOfSight()
        {
            if (blackboard.TryGetValue<bool>(hasSeenPlayerKey, out var seen) && seen)
                return true;

            bool hasLOS = IsInLos();
            if (hasLOS)
                blackboard.SetValue(hasSeenPlayerKey, true);

            return hasLOS;
        }




        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _los = GetComponent<LineOfSight>();
            _waypoints.Add(target);
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(0, 100);
            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;



            var controller = GetComponent<BlackboardController>();
            blackboard = controller.GetBlackboard();
            tree = BuildTree();
        }
        public void Tick() 
        {
            tree?.Process();
            UpdateTick();
        }

        protected virtual void UpdateTick() { }

        //------------ IDebuffReceiver Implementation ------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}

