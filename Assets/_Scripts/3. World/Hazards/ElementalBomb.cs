using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public sealed class ElementalBomb : MonoBehaviour, IHazard
    {
        [SerializeField] private ElementType _element;
        [SerializeField] private int _damage = 25;
        [SerializeField] private float _explosionRadius = 4f;
        [SerializeField] private GameObject _windupVFX;
        [SerializeField] private GameObject _explosionVFX;
        [SerializeField] private float _windupExplosion;

        private readonly int _explosionTriggerHash = Animator.StringToHash("t_Explosion");
        
        private bool _isActive = true;
        private bool _triggered;

        public bool TryDetonate(ElementType incomingElement)
        {
            if (!_isActive || _triggered) 
                return false;

            if (incomingElement != _element) 
                return false;

            _triggered = true;
            StartCoroutine(Explode());
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _triggered) 
                return;

            // Only player projectiles (IProjectile, not enemy) set off the bomb via physics.
            if (!other.TryGetComponent<IProjectile>(out var projectile)) 
                return;
            
            if (projectile.IsEnemy)
                return;
            
            TryDetonate(projectile.SpellElement);
        }

        private IEnumerator Explode()
        {
            var anim = GetComponentInChildren<Animator>();
            if (anim != null)
                anim.SetTrigger(_explosionTriggerHash);
            
            if (_windupVFX != null)
                Instantiate(_windupVFX, transform.position, Quaternion.identity);

            yield return CoroutineUtils.GetWait(_windupExplosion);
            
            if (_explosionVFX != null)
                Instantiate(_explosionVFX, transform.position, Quaternion.identity);
                
            var hits = Physics.OverlapSphere(new Vector3(transform.position.x, 0, transform.position.z), _explosionRadius);
            var processed = new HashSet<IDamageable>();

            var batch = new DamageBatch();
            
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
                    // Dashing: skip entirely.
                    if (!player.Hurtbox.activeSelf)
                        continue;

                    // Shielding: bomb triggers, shield is destroyed, no damage.
                    if (player.IsShielding)
                    {
                        player.ForceDestroyActiveShield();
                        continue;
                    }
                    
                    // Player takes Neutral damage explicitly
                    batch.Deal(damageable, go, _damage, ElementType.Neutral);
                }
                else
                {
                    // Enemies (and anything else) take Elemental damage
                    batch.Deal(damageable, go, _damage, _element);
                }
            }

            batch.Commit(Helpers.Combat.BombExplosion);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, 0, transform.position.z), _explosionRadius);
        }

        public void Disable()
        {
            _isActive = false;
        }
    }
}