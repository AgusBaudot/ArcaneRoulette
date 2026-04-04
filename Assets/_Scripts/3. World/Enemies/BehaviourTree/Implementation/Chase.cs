using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;

namespace world 
{
    public class Chase : IStrategy
    {
        readonly Transform _target;
        readonly NavMeshAgent _agent;
        readonly float _chaseSpeed;

        public Chase(Transform target, NavMeshAgent agent, float chaseSpeed) 
        {
            this._target = target;
            this._agent = agent;
            this._chaseSpeed = chaseSpeed;
        }
        public Node.NodeState Process()
        {  
            if (_target == null)
                return Node.NodeState.Failure;

            _agent.speed = _chaseSpeed;
            _agent.SetDestination(_target.position);

            if (_agent.pathPending)
                return Node.NodeState.Running;

            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return Node.NodeState.Failure;

            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                return Node.NodeState.Success;
            }

            return Node.NodeState.Running;
        }

        public void Reset()
        {
            if (_agent.hasPath)
                _agent.ResetPath();
        }
    }
}

