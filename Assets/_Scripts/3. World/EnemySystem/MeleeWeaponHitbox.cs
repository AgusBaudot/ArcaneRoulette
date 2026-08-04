using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Core;

namespace World
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MeleeWeaponHitbox : MonoBehaviour
    {
        private BoxCollider _collider;
        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        private int _damage;
        private ElementType _element;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        public void Configure(int damage, ElementType element, Vector3 size, Vector3 attackDirection)
        {
            _damage = damage;
            _element = element;
            _collider.size = size;

            // 1. Flatten the direction to the XZ plane to prevent the hitbox from tilting into the floor
            attackDirection.y = 0f;
            if (attackDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(attackDirection);
            }

            // 2. Offset the collider's center forward by half its Z-size.
            // This anchors the back edge of the box to the enemy, projecting it forward.
            _collider.center = new Vector3(0f, 0f, size.z / 2f);
        }

        public void Activate()
        {
            _hitThisSwing.Clear();
            _collider.enabled = true;
        }

        public void Deactivate()
        {
            _collider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<IDamageable>();
            if (target == null || _hitThisSwing.Contains(target))
                return;

            _hitThisSwing.Add(target);

            var batch = new DamageBatch();
            batch.Deal(target, _damage, _element);
            batch.Commit(Helpers.Combat.PlayerDamage);
        }

        private void OnDrawGizmos()
        {
            // Only draw when the hitbox is actually active (flashes during the swing)
            if (_collider != null && _collider.enabled)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                
                // Transparent red fill
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f); 
                Gizmos.DrawCube(_collider.center, _collider.size);
                
                // Solid red outline
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_collider.center, _collider.size);
            }
        }
    }
}