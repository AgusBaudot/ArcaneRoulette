using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace world 
{
    public class Attack : IStrategy
    {
        readonly float _cooldown;
        readonly Animation _animation;
        float cooldownTimer;
        bool isAttacking;
        bool inCooldown;

        public Attack(float Cooldown, Animation animation) 
        {
            this._cooldown = Cooldown;
            this._animation = animation;
        }

        public Node.NodeState Process() 
        {
            if (!isAttacking && !inCooldown) 
            {
                _animation.Play();
                isAttacking = true;
                return Node.NodeState.Running;
            }

            if (isAttacking)
            {
                if (_animation.isPlaying)
                    return Node.NodeState.Running;

                isAttacking = false;
                inCooldown = true;
                cooldownTimer = _cooldown;
            }

            if (inCooldown && _cooldown > 0)
            {
                cooldownTimer -= Time.deltaTime;

                if (cooldownTimer > 0)
                    return Node.NodeState.Running;

                inCooldown = false;
                return Node.NodeState.Success;
            }

            return Node.NodeState.Success;
        }

        public void Reset()
        {
            isAttacking = false;
            inCooldown = false;
            cooldownTimer = 0f;

            if (_animation.isPlaying)
                _animation.Stop();
        }
    }
}

