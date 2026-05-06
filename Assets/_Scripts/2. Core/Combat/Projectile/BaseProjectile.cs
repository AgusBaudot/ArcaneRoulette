using Foundation;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class BaseProjectile : MonoBehaviour, IProjectile, IPoolable
    {
        public Rigidbody Rb { get; private set; }
        public abstract bool IsEnemy { get; }
        public abstract ElementType SpellElement { get; }
        
        protected float Speed { get; private set; }
        protected int BounceCount;

        private static int _shieldLayer = -1;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Rb.useGravity = false;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            Rb.constraints = RigidbodyConstraints.FreezePositionY
                             | RigidbodyConstraints.FreezeRotation;
            
            if (_shieldLayer == -1)
                _shieldLayer = LayerMask.NameToLayer("Shield");
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
            
            foreach (var trail in GetComponentsInChildren<TrailRenderer>())
            {
                trail.Clear();
            }
        }

        //Returns true if the projectile bounced and should keep living.
        //Returns false if no bounces remain - called should Destroy.
        protected bool TryBounce()
        {
            if (BounceCount <= 0) 
                return false;
            
            //Step back half a unit along velocity so the ray starts before the surface
            Vector3 rayOrigin = transform.position - Rb.velocity.normalized * 0.5f + Vector3.up;

            if (Physics.Raycast(rayOrigin, Rb.velocity.normalized, out RaycastHit hit, 2f))
            {
                var reflected = Vector3.Reflect(Rb.velocity.normalized, hit.normal);
                reflected.y = 0f; //stay on XZ plane
                Rb.velocity = reflected.normalized * Speed;
                transform.forward = reflected.normalized;
            }
            else
            {
                //Ray missed - reverse direction as fallback rather than destroying
                Rb.velocity = -Rb.velocity;
                transform.forward = Rb.velocity.normalized;
                Debug.LogWarning("TryBounce: no surface found, reversing direction.");
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
            if (other.gameObject.layer == _shieldLayer || other.isTrigger)
                return;
            
            if (other.TryGetComponent<IDamageable>(out _))
                OnHitDamageable(other);
            else
                OnHitWall(other);
        }

        public virtual void OnSpawn()
        {
            if (Rb == null)
                return;
            
            Rb.position = transform.position;
            Rb.rotation = transform.rotation;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public virtual void OnDespawn()
        {
            if (Rb != null)
            {
                Rb.velocity = Vector3.zero;
                Rb.angularVelocity = Vector3.zero;
                Rb.interpolation = RigidbodyInterpolation.None;
            }
        }
    }
}