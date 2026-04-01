using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using World;

namespace world 
{
    [RequireComponent(typeof(LineOfSight))]
    [RequireComponent (typeof(NavMeshAgent))]
    public class AIBrain : MonoBehaviour
    {
        

        [Header("BehaviourTree")]
        [SerializeField] private float _patrolspeed;
        [SerializeField] private float _runningspeed;
        [SerializeField] private string _behaviourTreeName;
        private NavMeshAgent _agent;
        private BehaviourTree tree;
        private ConditionNode _root;
        private LineOfSight _los;
        public Blackboard _blackboard;


        public Transform target;
        public List<Transform> wayPoints;
        //private int currentWP = 0;

        void Awake() 
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _los = GetComponent<LineOfSight>();
            tree = new BehaviourTree(_behaviourTreeName);
            tree.AddChild(new TaskNode("Patrol", new Patrol(transform, _agent, wayPoints, _patrolspeed)));
        }
        private bool IsInLos() => _los.CheckRange(target) && _los.CheckAngle(target) && _los.CheckView(target);

        private void Update()
        {
            tree.Process();
        }
    }

    public class BehaviourTree : Node
    {
        public BehaviourTree(string name) : base(name) { }

        public override NodeState Process()
        {
            while (_currentChild < _children.Count) 
            {
                var status = _children[_currentChild].Process();
                if (status != NodeState.Success) 
                {
                    return status;
                }
                _currentChild++;
            }
            return NodeState.Success;
        }
    }
}

