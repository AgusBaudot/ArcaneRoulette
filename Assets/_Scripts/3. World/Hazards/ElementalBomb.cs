using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public sealed class ElementalBomb : MonoBehaviour
    {
        [SerializeField] private ElementType _element;
        [SerializeField] private int _damage = 25;
        [SerializeField] private float _explosionRadius = 4f;
        [SerializeField] private GameObject _windupVFX;
        [SerializeField] private GameObject _explosionVFX;
        [SerializeField] private float _windupExplosion;

        private bool _triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) 
                return;

            // Only player projectiles (IProjectile, not enemy) set off the bomb.
            if (!other.TryGetComponent<IProjectile>(out var projectile)) 
                return;
            
            if (projectile.IsEnemy)
                return;

            // Element must match the bomb's pre-imbued element.
            if (projectile.SpellElement != _element)
                return;

            _triggered = true;
            StartCoroutine(Explode());
        }

        private IEnumerator Explode()
        {
            if (_windupVFX != null)
                Instantiate(_windupVFX, transform.position, Quaternion.identity);

            yield return Helpers.GetWait(_windupExplosion);
            
            if (_explosionVFX != null)
                Instantiate(_explosionVFX, transform.position, Quaternion.identity);
                
            
            var hits = Physics.OverlapSphere(new Vector3(transform.position.x, 0, transform.position.z), _explosionRadius);
            var processed = new HashSet<IDamageable>();

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>()
                                 ?? hit.GetComponent<IDamageable>();
                
                if (damageable == null) 
                    continue;
                
                if (!processed.Add(damageable)) 
                    continue;

                var go = (damageable as Component)?.gameObject;
                var player = go?.GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    // Dashing — skip entirely.
                    if (!player.Hurtbox.activeSelf)
                        continue;

                    // Shielding — bomb triggers, shield is destroyed, no damage.
                    if (player.IsShielding)
                    {
                        player.ForceDestroyActiveShield();
                        continue;
                    }
                }

                // Friendly fire enabled — player takes elemental damage if not dashing/shielding.
                DamageSystem.Deal(damageable, go, _damage, _element);
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, 0, transform.position.z), _explosionRadius);
        }
    }
}