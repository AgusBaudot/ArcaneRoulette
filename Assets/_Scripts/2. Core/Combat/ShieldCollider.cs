using System;
using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShieldCollider : MonoBehaviour
    {
        [SerializeField] private Projectile _reflectedProjectilePrefab;
        [SerializeField] private float _reflectHitStop   = 0.05f;
        [SerializeField] private float _reflectTrauma    = 0.1f;

        public bool ReflectsProjectiles { get; set; }

        public event Action<Vector3, GameObject> OnProjectileAbsorbed;
        public event Action<Vector3, GameObject> OnEnemyBodyContact;

        private SpellInstance  _boundInstance;
        private MonoBehaviour  _runner;

        // Called once by StartHoldWithInstance after instantiation
        public void Bind(SpellInstance source, MonoBehaviour runner)
        {
            _boundInstance = source;
            _runner        = runner;
        }
        
        private void OnCollisionEnter(Collision other)
            =>  HandleContact(other.collider, other.contacts[0].point);

        private void OnTriggerEnter(Collider other)
            => HandleContact(other, other.transform.position);

        private void HandleContact(Collider other, Vector3 contactPoint)
        {
            if (!other.TryGetComponent<IProjectile>(out var projectile))
            {
                if (other.TryGetComponent<IDamageable>(out _))
                    OnEnemyBodyContact?.Invoke(contactPoint, other.gameObject);
                return;
            }

            if (!projectile.IsEnemy) return;

            if (ReflectsProjectiles
                && _boundInstance != null
                && _reflectedProjectilePrefab != null
                && other.TryGetComponent<IEnemyProjectile>(out var enemy))
            {
                var reflectedDir = -projectile.Rb.velocity.normalized;
                var speed        = projectile.Rb.velocity.magnitude;

                Destroy(other.gameObject);

                var friendly = Instantiate(
                    _reflectedProjectilePrefab,
                    other.transform.position,
                    Quaternion.identity);

                friendly.Init(
                    _boundInstance, reflectedDir, speed,
                    enemy.Damage, _reflectHitStop, _reflectTrauma, _runner);
            }
            else
            {
                Destroy(other.gameObject);
            }

            OnProjectileAbsorbed?.Invoke(transform.position, other.gameObject);
        }
    }
}