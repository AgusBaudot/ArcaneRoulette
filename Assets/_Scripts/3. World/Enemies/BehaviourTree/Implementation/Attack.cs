using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace World
{
    public class Attack : IStrategy
    {
        private readonly Animator _animator;
        private readonly float _cooldown;

        private bool _isAttacking;
        private float _nextAttackTime;

        public Attack(Animator animator, float cooldown)
        {
            _animator = animator;
            _cooldown = cooldown;
        }

        public Node.NodeState Process()
        {
            //  Cooldown
            if (Time.time < _nextAttackTime)
                return Node.NodeState.Failure;

            //  Si todavía no empezó el ataque
            if (!_isAttacking)
            {
                _animator.SetTrigger("Attack");
                _isAttacking = true;
                return Node.NodeState.Running;
            }

            //  Mientras la animación está corriendo
            if (IsAnimationPlaying())
                return Node.NodeState.Running;

            //  Terminó el ataque
            _isAttacking = false;
            _nextAttackTime = Time.time + _cooldown;

            return Node.NodeState.Success;
        }

        bool IsAnimationPlaying()
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("PlaceHolderAnimation") && state.normalizedTime < 1f;
        }

        public void Reset()
        {
            _isAttacking = false;
        }
    }
}

