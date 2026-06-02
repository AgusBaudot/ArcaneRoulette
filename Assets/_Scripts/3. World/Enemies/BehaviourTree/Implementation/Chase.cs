using UnityEngine;
using UnityEngine.AI;

namespace World 
{
    public class Chase : IStrategy
    {
        readonly Transform _target;
        readonly Transform _entity;
        readonly NavMeshAgent _agent;
        readonly float _chaseSpeed;

        public Chase(Transform target, Transform entity ,NavMeshAgent agent, float chaseSpeed) 
        {
            this._target = target;
            this._entity = entity;
            this._agent = agent;
            this._chaseSpeed = chaseSpeed;
        }
        public Node.NodeState Process()
        {  
            if (_target == null)
                return Node.NodeState.Failure;

            _agent.speed = _chaseSpeed;
            Vector3 offset = (_entity.position - _target.position).normalized * 1; //attackRange
            Vector3 targetPos = _target.position + offset;

            _agent.SetDestination(targetPos);

            if (_agent.pathPending)
                return Node.NodeState.Running;

            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return Node.NodeState.Failure;

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

