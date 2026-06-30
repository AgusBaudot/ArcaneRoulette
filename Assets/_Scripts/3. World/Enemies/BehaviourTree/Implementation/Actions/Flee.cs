using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using World;

namespace World
{
    public class Flee : IStrategy
    {
        private readonly Transform _target;
        private readonly Transform _entity;
        private readonly NavMeshAgent _agent;
        private readonly Func<float> _getFleeSpeed;
        private readonly Func<bool> _isTooCloseCondition;

        private Vector3 _currentDestination;
        private const float FLEE_DISTANCE = 5f;

        private float _fleeCooldownTimer = 0f;
        private const float FLEE_COOLDOWN_TIME = 1.0f;
        private bool _isFleeingCurrentRoute = false;

        public Flee(Transform target, Transform entity, NavMeshAgent agent, Func<float> getFleeSpeed, Func<bool> isTooCloseCondition)
        {
            _target = target;
            _entity = entity;
            _agent = agent;
            _getFleeSpeed = getFleeSpeed;
            _isTooCloseCondition = isTooCloseCondition;
        }

        public NodeState Process()
        {
            if (_target == null)
                return NodeState.Failure;

            if (!_isTooCloseCondition())
            {
                if (_isFleeingCurrentRoute) Reset();
                return NodeState.Failure;
            }

            if (_fleeCooldownTimer > 0f)
            {
                _fleeCooldownTimer -= Time.deltaTime;
                if (_isFleeingCurrentRoute) Reset();
                return NodeState.Failure;
            }

            _agent.speed = _getFleeSpeed();

            _agent.stoppingDistance = 1f;

            if (_isFleeingCurrentRoute && _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance + 0.3f)
            {
                return NodeState.Running;
            }

            // --- 360 radius to scape ---
            Vector3 desiredDirection = (_entity.position - _target.position).normalized;
            desiredDirection.y = 0;

            Vector3 bestFleePosition = Vector3.zero;
            bool foundValidPath = false;

            float[] angles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 140f, -140f, 160f, -160f };

            foreach (float angle in angles)
            {
                Vector3 rotatedDirection = Quaternion.Euler(0, angle, 0) * desiredDirection;
                Vector3 targetPoint = _entity.position + rotatedDirection * FLEE_DISTANCE;

                if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (_agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        bestFleePosition = hit.position;
                        foundValidPath = true;
                        break;
                    }
                }
            }

            if (foundValidPath)
            {
                _currentDestination = bestFleePosition;
                _agent.SetDestination(_currentDestination);
                _isFleeingCurrentRoute = true;
                return NodeState.Running;
            }
            else
            {
                Reset();
                _fleeCooldownTimer = FLEE_COOLDOWN_TIME;
                return NodeState.Failure;
            }
        }

        public void Reset()
        {
            _isFleeingCurrentRoute = false;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
        }
    }
}

