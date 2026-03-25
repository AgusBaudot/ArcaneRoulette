using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World;

namespace world 
{
    public class AIBrain
    {
        [Header("Variables")]
        public LineOfSight _los;
        private float _speed;
        public Blackboard _blackboard;
        public Transform _thisEnemy;

        [Header("BehaviourTree")]
        private string _behaviourTreeName;
        BehaviourTree tree;
        private ConditionNode _root;


        public Transform target;
        public Transform[] wayPoints;
        private int currentWP = 0;

        public AIBrain(LineOfSight los, Transform thisEnemy, string behaviourTreeName)
        {
            _los = los;
            _thisEnemy = thisEnemy;
            _behaviourTreeName = behaviourTreeName;
            InitBT();
        }
        void InitBT() 
        {
            tree = new BehaviourTree(_behaviourTreeName);
        }
        private bool IsInLos() => _los.CheckRange(target) && _los.CheckAngle(target) && _los.CheckView(target);
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

