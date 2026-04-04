using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using World;

namespace world 
{
    [RequireComponent(typeof(LineOfSight))]
    [RequireComponent (typeof(NavMeshAgent))]
    public abstract class AIBrain : MonoBehaviour
    {
        [Header("Components Reference")]
        [SerializeField] protected BehaviourTree tree;
        [SerializeField] protected NavMeshAgent _agent;
        [SerializeField] protected LineOfSight _los;
        [Header("Common AI Data")]
        [SerializeField] protected Transform target;
        [SerializeField] protected string _behaviourTreeName;
        //public List<Transform> wayPoints;


        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _los = GetComponent<LineOfSight>();
            tree = BuildTree();
        }
        protected virtual void Update()
        {
            tree?.Process();
        }
        protected abstract BehaviourTree BuildTree();
        protected virtual bool IsInLos() //virtual para poder anularlo
        {
            if (_los == null || target == null) return false;
            return _los.CheckRange(target) &&
                   _los.CheckAngle(target) &&
                   _los.CheckView(target);
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
            //Reset(); por testear
            return NodeState.Success;
        }
    }
}

