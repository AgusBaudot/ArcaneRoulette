using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace world 
{
    public class Patrol : IStrategy
    {
        readonly Transform _entity;
        readonly NavMeshAgent _agent;
        readonly List<Transform> _patrolPoints;
        readonly float _patrolSpeed;
        int _currentIndex = 0;
        bool _isPathCalculated = true; // para navmesh

        public Patrol(Transform entity, NavMeshAgent agent, List<Transform> patrolPoints, float patrolSpeed)
        {
            this._entity = entity;
            this._agent = agent;
            this._patrolPoints = patrolPoints;
            this._patrolSpeed = patrolSpeed;
        }

        public Node.NodeState Process() 
        {
            _agent.speed = _patrolSpeed;
            _currentIndex = (_currentIndex + 1) % _patrolPoints.Count;
            if (_currentIndex == _patrolPoints.Count) return Node.NodeState.Success;

            var target = _patrolPoints[_currentIndex];
            _agent.SetDestination(target.position);

            float distance = (_patrolPoints[_currentIndex].position - _entity.position).sqrMagnitude;

            if (_isPathCalculated && distance < 0.5f)
            {
                _currentIndex++;
               _isPathCalculated = false;
            }


            if (_agent.pathPending) 
            {
                _isPathCalculated = true;
            }
             //= _agent.pathPending;

            return Node.NodeState.Running;
        }

        public void Reset() => _currentIndex = 0;
    } 
}

