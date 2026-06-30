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
        private readonly float _attackRange;

        public Chase(Transform target, Transform entity ,NavMeshAgent agent, Func<float> getChaseSpeed, float attackRange) 
        {
            _target = target;
            _entity = entity;
            _agent = agent;
            _getChaseSpeed = getChaseSpeed;
            _attackRange = attackRange;
        }
        public NodeState Process()
        {  
            if (_target == null)
                return NodeState.Failure;

            _agent.speed = _getChaseSpeed();
            _agent.stoppingDistance = _attackRange - 0.5f;

            _agent.SetDestination(_target.position);
            float realDistance = Vector3.Distance(_entity.position, _target.position);

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
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

