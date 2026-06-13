using System;
using UnityEngine;
using UnityEngine.AI;

namespace World 
{
    public class FollowAlly : IStrategy
    {
        readonly Transform _entity;
        readonly NavMeshAgent _agent;
        readonly Func<float> _getFollowSpeed;
        readonly Func<Transform> _ally;
        public FollowAlly(Func<Transform> ally, Transform entity, NavMeshAgent agent, Func<float> getFollowSpeed)
        {
            this._ally = ally;
            this._entity = entity;
            this._agent = agent;
            _getFollowSpeed = getFollowSpeed;
        }
        public Node.NodeState Process()
        {
            if (_ally == null)
                return Node.NodeState.Failure;

            _agent.speed = _getFollowSpeed();
            Vector3 offset = (_entity.position - _ally().position).normalized;
            Vector3 targetPos = _ally().position + offset;

            _agent.SetDestination(_ally().position);

            if (_agent.pathPending)
                return Node.NodeState.Running;

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
