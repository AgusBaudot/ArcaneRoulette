using UnityEngine;
using UnityEngine.AI;
using Foundation;

namespace World 
{
    [RequireComponent(typeof(LineOfSight))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public abstract class AIBrain : MonoBehaviour, IDebuffReceiver
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

        protected IDebuffReadable _debuffs;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(0, 100);
            _los = GetComponent<LineOfSight>();
            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;

            var controller = GetComponent<BlackboardController>();
            blackboard = controller.GetBlackboard();
            tree = BuildTree();
        }
        protected virtual void Update()
        {
            tree?.Process();
        }
        protected abstract BehaviourTree BuildTree();
        protected virtual bool IsInLos()
        {
            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;
            if (_los == null || target == null) return false;
            return _los.CheckRange(target) && _los.CheckView(target);
        }
        
        //IDebuffReceiver Implementation ------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }

    public class BehaviourTree : Node
    {
        public BehaviourTree(string name) : base(name) { }

        public override NodeState Process()
        {

            if (_children.Count == 0)
                return NodeState.Failure;

            return _children[0].Process();

            /*
            while (_currentChild < _children.Count) 
            {
                var status = _children[_currentChild].Process();
                if (status != NodeState.Success) 
                {
                    return status;
                }
                _currentChild++;
            }
            //Reset(); //solo test
            Debug.Log("Termino???");
            return NodeState.Success;*/
        }
        /*
        public override NodeState Process()
        {
            NodeState status = _children[_currentChild].Process();

            _currentChild = (_currentChild + 1) % _children.Count;
            return NodeState.Running;
        }*/
    }
}

