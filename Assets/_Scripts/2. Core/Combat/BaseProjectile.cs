using System;
using Foundation;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class BaseProjectile : MonoBehaviour, IProjectile
    {
        public Rigidbody Rb { get; private set; }
        public abstract bool IsEnemy { get; }
        
        protected float Speed { get; private set; }
        protected int BounceCount;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Rb.useGravity = false;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            Rb.constraints = RigidbodyConstraints.FreezePositionY
                             | RigidbodyConstraints.FreezeRotation;
        }

        protected void SetVelocity(Vector3 direction, float speed)
        {
            Speed = speed;
            Rb.velocity = direction.normalized * speed;
            transform.forward = direction.normalized;
        }

        protected void PlayParticles()
        {
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                ps.Clear();
                ps.Play();
            }
        }

        //Returns true if the projectile bounced and should keep living.
        //Returns false if no bounces remain - called should Destroy.
        protected bool TryBounce()
        {
            if (BounceCount <= 0) return false;

            if (Physics.Raycast(transform.position, Rb.velocity.normalized, out RaycastHit hit, 1f))
            {
                var reflected = Vector3.Reflect(Rb.velocity.normalized, hit.normal);
                Rb.velocity = reflected * Speed;
                transform.forward = reflected;
            }

            BounceCount--;
            return true;
        }

        //Subclasses implement what happens on each collision type
        protected abstract void OnHitDamageable(Collider other);
        protected abstract void OnHitWall(Collider other);

        //Subclasses never override this - they implement the two semantic methods above instead. 
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IDamageable>(out _))
                OnHitDamageable(other);
            else
                OnHitWall(other);
        }
    }
}