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
        //readonly float _chaseSpeed;

        public Chase(Transform target, Transform entity ,NavMeshAgent agent, Func<float> getChaseSpeed) 
        {
            this._target = target;
            this._entity = entity;
            this._agent = agent;
            _getChaseSpeed = getChaseSpeed;
        }
        readonly Func<float> _getChaseSpeed;
        public Node.NodeState Process()
        {  
            if (_target == null)
                return Node.NodeState.Failure;

            _agent.speed = _getChaseSpeed();
            Vector3 offset = (_entity.position - _target.position).normalized * 1; //attackRange
            Vector3 targetPos = _target.position + offset;

            _agent.SetDestination(targetPos);

            if (_agent.pathPending)
                return Node.NodeState.Running;

            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                return Node.NodeState.Failure; 
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

