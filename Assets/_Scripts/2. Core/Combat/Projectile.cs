using System;
using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [Header("Juice on impact")]
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.5f;
        [SerializeField] private float _knockbackForce = 9f;
        [SerializeField] private GameObject _impactPrefab;
        
        private Vector3 _direction;
        private PlayerStats _playerStats;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth at any framerate
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotation;
        }

        public void Initialize(Vector3 direction, float speed, PlayerStats stats)
        {
            _direction = direction.normalized;
            _playerStats = stats;

            _rb.velocity = _direction * speed;

            if (_direction != Vector3.zero)
            {
                transform.forward = _direction;
            }

            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                ps.Clear();
                ps.Play();
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            Instantiate(_impactPrefab, _rb.position, Quaternion.identity);
            if (!other.gameObject.TryGetComponent<IDamageable>(out var damageable))
            {
                Destroy(gameObject);
                return;
            }

            damageable.TakeDamage(_playerStats.BaseDamage, ElementType.Fire);

            //Knockback - enemy needs a Rigidbody for this to work.
            if (other.gameObject.TryGetComponent<IKnockbackable>(out var kb))
                kb.ApplyKnockback(_direction, _knockbackForce);
            
            //Damage flash
            if (other.gameObject.TryGetComponent<DamageFlash>(out var flash))
                flash.Flash();
            
            //The juice
            HitStop.Apply(_hitStopDuration);
            CameraShake.AddTrauma(_cameraTrauma);

            Destroy(gameObject);
        }
    }
}