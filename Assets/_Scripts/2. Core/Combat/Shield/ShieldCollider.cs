using System;
using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShieldCollider : MonoBehaviour
    {
        [SerializeField] private Projectile _reflectedProjectilePrefab;

        public bool ReflectsProjectiles { get; set; }
        public int ReflectCount { get; set; } //set by BounceCastRune stack count
        public float ReflectSpread { get; set; } = 0f; //0 = single direction
        
        public event Action<Vector3, GameObject> OnProjectileAbsorbed;
        public event Action<Vector3, GameObject> OnEnemyBodyContact;
        public event Action OnShieldDamaged;

        private SpellInstance  _boundInstance;
        private MonoBehaviour  _runner;

        public void Bind(SpellInstance source, MonoBehaviour runner)
        {
            _boundInstance = source;
            _runner = runner;
        }

        private void OnCollisionEnter(Collision other)
            =>  HandleContact(other.collider, other.contacts[0].point);

        private void OnTriggerEnter(Collider other)
            => HandleContact(other, (transform.position + other.transform.position) * 0.5f);

        private void HandleContact(Collider other, Vector3 contactPoint)
        {
            if (!other.TryGetComponent<IProjectile>(out var projectile))
            {
                if (other.TryGetComponent<IDamageable>(out _))
                {
                    OnEnemyBodyContact?.Invoke(contactPoint, other.gameObject);
                    OnShieldDamaged?.Invoke();
                }
                else
                {
                    var destructible = other.GetComponentInParent<IDestructible>();
                    
                    if (destructible != null && destructible.IsDestroyed)
                    {
                        return;
                    }

                    destructible?.OnDeath(contactPoint);
                    OnShieldDamaged?.Invoke();
                }
                return;
            }

            if (!projectile.IsEnemy)
                return;

            OnShieldDamaged?.Invoke();

            other.TryGetComponent<IEnemyProjectile>(out var enemy);
            
            if (ReflectsProjectiles
                && _boundInstance != null
                && _reflectedProjectilePrefab != null
                && enemy != null)
            {
                Debug.Log("Reflecting");
                Vector3 reflectBase = -projectile.Rb.velocity.normalized;
                reflectBase.y = 0;
                
                float speed = projectile.Rb.velocity.magnitude;
                var dirs = ReflectionUtils.GetSpreadDirections(
                    reflectBase, ReflectCount, ReflectSpread);

                foreach (var d in dirs)
                {
                    var go = Helpers.ProjFactory.Spawn(_reflectedProjectilePrefab, other.transform.position, Quaternion.LookRotation(d));
                    go.Init(_boundInstance, d, speed, enemy.Damage, _runner, AbilityType.Projectile, true);
                    go.SetPierceCount(0);
                    go.SetBounceCount(0);
                }
                
                Destroy(other.gameObject);
            }
            else
            {
                Debug.Log("Absorbing");
                GameObject onHitTarget = enemy?.Owner != null
                    ? enemy.Owner
                    : (enemy as Component)?.gameObject ?? other.gameObject;

                OnProjectileAbsorbed?.Invoke(contactPoint, onHitTarget);
            
                Destroy(other.gameObject);
            }
        }

        public void UnsubscribeListeners()
        {
            OnProjectileAbsorbed = null;
            OnEnemyBodyContact = null;
            OnShieldDamaged = null;
        }
    }
}