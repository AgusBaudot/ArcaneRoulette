using UnityEngine;
using Core;
using Foundation;
using System.Collections.Generic;

namespace World
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BruteThrustHitbox : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _telegraphVisual;
        
        private BoxCollider _col;
        private HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private int _damage;
        
        private float _maxDistance;
        private float _currentDistance;

        private void Awake()
        {
            _col = GetComponent<BoxCollider>();
            _col.isTrigger = true;
            Deactivate();
        }

        public void ShowTelegraph(BruteEnemyStats stats, Vector3 direction)
        {
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            _telegraphVisual.enabled = true;
            
            // Assuming the sprite is rotated 90 on X, its local Y axis maps to the Z axis length.
            _telegraphVisual.transform.localScale = new Vector3(_col.size.x, stats.ThrustHitboxMaxDistance, 1f);
            
            // Offset so the bottom edge sits at the Brute's feet and it stretches forward.
            // Y-offset of 0.05f prevents Z-fighting with the floor mesh.
            _telegraphVisual.transform.localPosition = new Vector3(0f, 0.05f, stats.ThrustHitboxMaxDistance / 2f);
        }

        public void Activate(BruteEnemyStats stats, int damage, Vector3 direction)
        {
            _telegraphVisual.enabled = false; 
            
            _damage = damage;
            _maxDistance = stats.ThrustHitboxMaxDistance;
            _currentDistance = 0f;
            _hitTargets.Clear();
            _col.enabled = true;
    
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
    
            // Reset box to start position
            _col.size = new Vector3(_col.size.x, _col.size.y, 0.1f);
            _col.center = new Vector3(0, _col.center.y, 0);
        }

        public void UpdateExpansion(float dt, float attackSpeed)
        {
            if (!_col.enabled) return;

            // Expand forwards based on attack speed
            _currentDistance = Mathf.MoveTowards(_currentDistance, _maxDistance, attackSpeed * dt);
            
            _col.size = new Vector3(_col.size.x, _col.size.y, _currentDistance);
            _col.center = new Vector3(0, _col.center.y, _currentDistance / 2f);
        }

        public void Deactivate()
        {
            _col.enabled = false;
            if (_telegraphVisual != null) _telegraphVisual.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && _hitTargets.Add(damageable))
            {
                var batch = new DamageBatch();
                batch.Deal(damageable, _damage, ElementType.Neutral);
                batch.Commit(Helpers.Combat.PlayerDamage);
            }
        }
    }
}