using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using World;

namespace world 
{
    [RequireComponent(typeof(LineOfSight))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public abstract class AIBrain : MonoBehaviour
    {
        [Header("Components Reference")]
        [SerializeField] protected Animator _animator;
        [SerializeField] protected NavMeshAgent _agent;
        [SerializeField] protected LineOfSight _los;
        [Header("Common AI Data")]
        [SerializeField] protected Blackboard blackboard;
        [SerializeField] protected BlackboardController blackboardController;
        [SerializeField] protected BehaviourTree tree;
        [SerializeField] protected Transform target;
        [SerializeField] protected string _behaviourTreeName;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _los = GetComponent<LineOfSight>();
            blackboardController = GetComponent<BlackboardController>();
            blackboard = blackboardController.GetBlackboard();
            tree = BuildTree();
            blackboard = BuildBlackboard();
        }
        protected virtual void Update()
        {
            tree?.Process();
        }
        protected abstract BehaviourTree BuildTree();
        protected abstract Blackboard BuildBlackboard();
        protected virtual bool IsInLos()
        {
            if (_los == null || target == null) return false;
            return _los.CheckRange(target) && _los.CheckView(target);
        }

        protected virtual bool IsInRange(float attackRange) 
        {
            return Vector3.Distance(transform.position, target.position) <= attackRange;
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
            //Reset(); //solo test
            return NodeState.Success;
        }
    }
}

