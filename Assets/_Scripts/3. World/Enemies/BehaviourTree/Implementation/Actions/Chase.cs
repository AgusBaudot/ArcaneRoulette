using System;
using UnityEngine;
using UnityEngine.AI;

namespace World 
{
    public class Chase : IStrategy
    {
        readonly Transform _target;
        readonly Transform _entity;
        readonly NavMeshAgent _agent;
        readonly Func<float> _getChaseSpeed;

        public Chase(Transform target, Transform entity ,NavMeshAgent agent, Func<float> getChaseSpeed) 
        {
            _target = target;
            _entity = entity;
            _agent = agent;
            _getChaseSpeed = getChaseSpeed;
        }
        public NodeState Process()
        {  
            if (_target == null)
                return NodeState.Failure;

            _agent.speed = _getChaseSpeed();
            Vector3 offset = (_entity.position - _target.position).normalized;
            Vector3 targetPos = _target.position + offset;

            _agent.SetDestination(_target.position);

            if (_agent.pathPending)
                return NodeState.Running;

            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                return NodeState.Success; 
            }

            return NodeState.Running;
        }

        public void Reset()
        {
            if (_agent.hasPath)
                _agent.ResetPath();
        }
    }
}

