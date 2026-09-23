using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Core;

namespace World
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MeleeWeaponHitbox : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;

        private BoxCollider _collider;
        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        private int _damage;
        private ElementType _element;

        private Quaternion _startRotation;
        private Quaternion _endRotation;
        private float _sweepDuration;
        private float _sweepProgress;
        private bool _isSweeping;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
        }

        public void Configure(int damage, ElementType element, Vector3 size, Vector3 attackDirection, float sweepAngle)
        {
            _damage = damage;
            _element = element;
            _collider.size = size;

            attackDirection.y = 0f;
            if (attackDirection != Vector3.zero)
            {
                Quaternion baseRotation = Quaternion.LookRotation(attackDirection);
                
                _startRotation = baseRotation * Quaternion.Euler(0f, -sweepAngle / 2f, 0f);
                _endRotation = baseRotation * Quaternion.Euler(0f, sweepAngle / 2f, 0f);
                
                transform.rotation = _startRotation;
            }

            _collider.center = new Vector3(0f, 0f, size.z / 2f);
        }

        public void Activate(float swingDuration)
        {
            _hitThisSwing.Clear();
            _collider.enabled = true;
            
            _sweepDuration = Mathf.Max(0.01f, swingDuration);
            _sweepProgress = 0f;
            _isSweeping = true;
        }

        public void Deactivate()
        {
            _collider.enabled = false;
            _isSweeping = false;
        }

        public void Tick(float dt)
        {
            if (!_isSweeping || !_collider.enabled) return;

            _sweepProgress += dt / _sweepDuration;
            transform.rotation = Quaternion.Slerp(_startRotation, _endRotation, Mathf.Clamp01(_sweepProgress));

            if (_sweepProgress >= 1f)
            {
                _isSweeping = false;
            }
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
            if (_collider != null && _collider.enabled)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f); 
                Gizmos.DrawCube(_collider.center, _collider.size);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_collider.center, _collider.size);
            }
        }
    }
}