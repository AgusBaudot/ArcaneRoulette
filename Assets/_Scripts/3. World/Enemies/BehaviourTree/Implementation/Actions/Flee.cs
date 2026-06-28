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
        readonly Transform _target;
        readonly Transform _entity;
        readonly NavMeshAgent _agent;
        readonly Func<float> _getChaseSpeed;

        public Flee (Transform target, Transform entity, NavMeshAgent agent, Func<float> getChaseSpeed) 
        {
            _target = target;
            _entity = entity;
            _agent = agent;
            _getChaseSpeed = getChaseSpeed;
        }
        public NodeState Process()
        {
            if(_target == null)
                return NodeState.Failure;

            return NodeState.Failure; // progresss
        }
        public void Reset()
        {
            if (_agent.hasPath)
                _agent.ResetPath();
        }
    }
}

