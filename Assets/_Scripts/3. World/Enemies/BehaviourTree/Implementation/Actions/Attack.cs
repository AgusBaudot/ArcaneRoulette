using System;
using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    public class Attack : IStrategy
    {
        private readonly Animator _animator;
        private readonly string _attackAnimName;
        private readonly NavMeshAgent _agent;

        private bool _isAttacking;
        private float _nextAttackTime;

        public Attack(Animator animator, NavMeshAgent agent ,Func<float> getcooldown, string attackAnimName)
        {
            _animator = animator;
            _agent = agent;
            _getCooldown = getcooldown;
            _attackAnimName = attackAnimName;
        }
        readonly Func<float> _getCooldown;
        public Node.NodeState Process()
        {
            //  Cooldown
            if (Time.time < _nextAttackTime)
                return Node.NodeState.Running;

            if (!_isAttacking)
            {
                _animator.SetTrigger("Attack");
                _isAttacking = true;
                _agent.ResetPath();
                return Node.NodeState.Running;
            }

            if (IsAnimationPlaying())
                return Node.NodeState.Running;
            _isAttacking = false;
            _nextAttackTime = Time.time + _getCooldown();

            return Node.NodeState.Success;
        }

        bool IsAnimationPlaying()
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName(_attackAnimName) && state.normalizedTime < 1f;
        }

        public void Reset()
        {
            _isAttacking = false;
        }
    }
}

