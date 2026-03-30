using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World;

namespace world 
{
    [RequireComponent(typeof(LineOfSight))]
    public class AIBrain : MonoBehaviour
    {
        [Header("Variables")]
        [SerializeField] private float _patrolspeed;
        [SerializeField] private float _runningspeed;
        public Blackboard _blackboard;

        [Header("BehaviourTree")]
        [SerializeField] private string _behaviourTreeName;
        BehaviourTree tree;
        private ConditionNode _root;
        private LineOfSight _los;


        public Transform target;
        public List<Transform> wayPoints;
        private int currentWP = 0;

        void Awake() 
        {
            _los = GetComponent<LineOfSight>();
            tree = new BehaviourTree(_behaviourTreeName);
            tree.AddChild(new TaskNode("Patrol", new Patrol(transform, wayPoints, _patrolspeed)));
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

