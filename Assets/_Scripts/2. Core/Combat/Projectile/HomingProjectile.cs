using Foundation;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class HomingProjectile : BaseProjectile, IFixedUpdatable
    {
        public int FixedUpdatePriority => UpdatePriority.Projectile;
        public override bool IsEnemy => false;
        public override ElementType SpellElement => _element;

        [SerializeField] private float _seekRadius = 20f;
        [SerializeField] private float _turnSpeed = 6f; //Degrees interpolation per second.

        private int _damage;
        private ElementType _element;
        private float _speed;
        private Transform _currentTarget;
        
        //Layer mask cached once - avoids string lookup every FixedUpdate
        private static int _enemyLayerMask;

        private void OnEnable() => UpdateManager.Instance.Register(this);
        private void OnDisable() => UpdateManager.Instance?.Unregister(this);

        public void Init(Vector3 initialDirection, float speed, int damage, ElementType element)
        {
            _damage = damage;
            _element = element;
            _speed = speed;
            _enemyLayerMask = LayerMask.GetMask("Enemy");

            SetVelocity(initialDirection, speed);
            PlayParticles();
        }

        public void FixedTick(float deltaTime)
        {
            AcquireOrRevalidateTarget();

            if (_currentTarget == null)
            {
                //No enemies in range - fly straight.
                return;
            }
            
            SteerToward(_currentTarget.position);
        }

        private void AcquireOrRevalidateTarget()
        {
            // If current target is still valid, keep it — no unnecessary OverlapSphere
            if (_currentTarget != null &&
                _currentTarget.gameObject.activeInHierarchy &&
                _currentTarget.GetComponent<IDamageable>() != null)
                return;

            _currentTarget = null;
            FindNearestEnemy();
        }

        private void FindNearestEnemy()
        {
            var hits = Physics.OverlapSphere(transform.position, _seekRadius, _enemyLayerMask);

            float bestDist = float.MaxValue;
            Transform best = null;

            foreach (var hit in hits)
            {
                // Only target valid IDamageable GameObjects
                if (hit.GetComponentInParent<IDamageable>() == null)
                    continue;

                float dist = (hit.transform.position - transform.position).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = hit.transform;
                }
            }

            _currentTarget = best;
        }

        private void SteerToward(Vector3 targetPosition)
        {
            Vector3 desiredDir = (targetPosition - transform.position);
            desiredDir.y = 0f;
            if (desiredDir == Vector3.zero) return;
            desiredDir.Normalize();

            // Gradual steering — slerp current velocity direction toward desired
            Vector3 steered = Vector3.Slerp(
                Rb.velocity.normalized,
                desiredDir,
                _turnSpeed * Time.fixedDeltaTime);

            steered.y = 0f;
            steered.Normalize();

            Rb.velocity = steered * _speed;
            transform.forward = steered;
        }

        protected override void OnHitDamageable(Collider other)
        {
            var damageable = other.GetComponentInParent<IDamageable>(true)
                             ?? other.GetComponent<IDamageable>();
            if (damageable == null) return;

            // No OnHit runes, no pierce, no bounce — just damage and destroy.
            // DamageSystem.Deal without DamageJuice uses Default internally.
            DamageSystem.Deal(damageable, other.gameObject, _damage, _element, DamageJuice.Light);
            Destroy(gameObject);
        }

        protected override void OnHitWall(Collider other)
        {
            // Homing projectiles don't bounce — destroy on wall contact.
            Destroy(gameObject);
        }
    }
}