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
        
        //Situation 1: projectile absorbed (no bounce) - fire OnHit on projectile owner
        //Situation 3: enemy body contact - fire OnHit on enemy
        //Situation 2 has no event - reflected projectile fires OnHit when it hits enemy
        public event Action<Vector3, GameObject> OnProjectileAbsorbed;
        public event Action<Vector3, GameObject> OnEnemyBodyContact;
        public event Action OnShieldDamaged;

        private SpellInstance  _boundInstance;
        private MonoBehaviour  _runner;

        // Called once by StartHoldWithInstance after instantiation
        public void Bind(SpellInstance source, MonoBehaviour runner)
        {
            _boundInstance = source;
            _runner        = runner;
        }

        //Solid collider - enemy projectiles and bodies hit this
        private void OnCollisionEnter(Collision other)
            =>  HandleContact(other.collider, other.contacts[0].point);

        //Trigger mode - active when AllowEnemyThrough (Piercing + Shield)
        private void OnTriggerEnter(Collider other)
            => HandleContact(other, (transform.position + other.transform.position) * 0.5f);

        private void HandleContact(Collider other, Vector3 contactPoint)
        {
            if (!other.TryGetComponent<IProjectile>(out var projectile))
            {
                //Situation 3 - enemy body contact
                //Bounce has no meaning here per designer spec - fire all OnHit runes
                if (other.TryGetComponent<IDamageable>(out _))
                {
                    OnEnemyBodyContact?.Invoke(contactPoint, other.gameObject);
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
                //Situation 2 - reflect
                //Do not fire OnProjectileAbsorbed here.
                //OnHit fires when the reflected projectile hits an enemy (excludeBounceCastRune = true)
                Vector3 reflectBase = -projectile.Rb.velocity.normalized;
                reflectBase.y = 0;
                
                float speed = projectile.Rb.velocity.magnitude;
                var dirs = ReflectionUtils.GetSpreadDirections(
                    reflectBase, ReflectCount, ReflectSpread);

                foreach (var d in dirs)
                {
                    var go = Instantiate(_reflectedProjectilePrefab, other.transform.position, Quaternion.LookRotation(d));
                    go.Init(_boundInstance, d, speed, enemy.Damage, _runner, AbilityType.Projectile, true);
                    go.SetPierceCount(0);
                    go.SetBounceCount(0);
                }
                
                Destroy(other.gameObject);
                //No event - situation 2 OnHit resolves on reflected projectile impact
            }
            else
            {
                Debug.Log("Absorbing");
                //Situation 1 - absorb
                //Fore OnHit on the enemy that fired the projectile (Owner).
                //Falls back to projectile position/GO if Owner is null (EnemyAI not yet wired).
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