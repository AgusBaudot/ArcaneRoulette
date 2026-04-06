using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace world 
{
    public class Attack : IStrategy
    {
        readonly float _cooldown;
        readonly Animator _animator;

        float cooldownTimer;
        bool isAttacking;
        bool inCooldown;

        public Attack(float cooldown, Animator animator)
        {
            _cooldown = cooldown;
            _animator = animator;
        }

        public Node.NodeState Process()
        {
            if (isAttacking)
            {
                if (IsAnimationPlaying())
                    return Node.NodeState.Running;

                isAttacking = false;
                inCooldown = true;
                cooldownTimer = _cooldown;
            }

            if (inCooldown)
            {
                cooldownTimer -= Time.deltaTime;

                if (cooldownTimer > 0)
                    return Node.NodeState.Running;

                inCooldown = false;
                return Node.NodeState.Success;
            }
            _animator.SetTrigger("Attack");
            isAttacking = true;
            return Node.NodeState.Running;
        }

        bool IsAnimationPlaying()
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("Attack") && state.normalizedTime < 1f;
        }

        public void Reset()
        {
            isAttacking = false;
            inCooldown = false;
            cooldownTimer = 0f;
        }
    }
}

