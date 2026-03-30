using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace world 
{
    public class Patrol : IStrategy
    {
        readonly Transform _entity;
        //readonly NavMeshAgent agent;
        readonly List<Transform> _patrolPoints;
        readonly float _patrolSpeed;
        int _currentIndex;
        bool _isPathCalculated = true; // para navmesh

        public Patrol(Transform entity, List<Transform> patrolPoints, float patrolSpeed)
        {
            _entity = entity;
            _patrolPoints = patrolPoints;
            _patrolSpeed = patrolSpeed;
        }

        public Node.NodeState Process() 
        {
            if (_currentIndex == _patrolPoints.Count) return Node.NodeState.Success;

            var target = _patrolPoints[_currentIndex];
            _entity.LookAt(target);

            float distance = (_patrolPoints[_currentIndex].position - _entity.position).sqrMagnitude;

            if (_isPathCalculated && distance < 0.5f) 
            {
                _currentIndex++;
               // _isPathCalculated = false; para nav mesh
            }
            
            return Node.NodeState.Running;
        }

        public void Reset() => _currentIndex = 0;
    } 
}

