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
        public int ReflectCount { get; set; } //set by BounceCastRune stack count
        public float ReflectSpread { get; set; } = 0f; //0 = single direction

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
                Vector3 incomingDir = projectile.Rb.velocity.normalized;
                Vector3 reflectBase = Vector3.Reflect(incomingDir, Vector3.forward);
                reflectBase.y = 0;

                float speed = projectile.Rb.velocity.magnitude;
                var dirs = ReflectionUtils.GetSpreadDirections(
                    reflectBase, ReflectCount, ReflectSpread);

                foreach (var d in dirs)
                {
                    var go = Instantiate(_reflectedProjectilePrefab, other.transform.position, Quaternion.LookRotation(d));
                    go.Init(_boundInstance, d, speed, enemy.Damage, _reflectHitStop, _reflectTrauma, _runner, AbilityType.Projectile, true);
                    go.SetPierceCount(0);
                    go.SetBounceCount(0);
                }
                
                Destroy(other.gameObject);
            }
            else
            {
                Destroy(other.gameObject);
            }
            
            OnProjectileAbsorbed?.Invoke(contactPoint, other.gameObject);
        }
    }
}