using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace world 
{
    public class Attack : IStrategy
    {
        private readonly Animator _animator;
        bool isAttacking;


        public Attack(Animator animator)
        {
            _animator = animator;
        }

        public Node.NodeState Process()
        {
            if (isAttacking)
            {
                if (IsAnimationPlaying())
                    return Node.NodeState.Running;

                return Node.NodeState.Success;
            }

            _animator.SetTrigger("Attack");
            isAttacking = true;

            return Node.NodeState.Running;
        }

        bool IsAnimationPlaying()
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("PlaceHolderAnimation") && state.normalizedTime < 1f;
        }

        public void Reset()
        {
            isAttacking = false;
        }
    }
}

