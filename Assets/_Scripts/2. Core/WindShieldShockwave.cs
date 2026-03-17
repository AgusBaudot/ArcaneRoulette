using System;
using Foundation;
using UnityEngine;

namespace Core
{
    public class WindShieldShockwave : MonoBehaviour
    {
        [SerializeField] private float _knockbackForce;

        private float _shieldLifeTime;
        private SphereCollider _collider;

        private void Start()
        {
            _collider = GetComponent<SphereCollider>();
            Destroy(gameObject, 1f);
        }

        private void Update()
        {
            _shieldLifeTime += Time.deltaTime;
            float percent = _shieldLifeTime / 0.6f; //0.6 is the total duration of the VFX
            _collider.radius = Mathf.Lerp(0, 5, percent);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable) && !other.CompareTag("Player"))
            {
                Debug.Log("Wire it with windshield to receive damage info");
                damageable.TakeDamage(2, ElementType.Wind);
                if (other.TryGetComponent<DamageFlash>(out var flash))
                    flash.Flash();
            }

            if (other.TryGetComponent<IKnockbackable>(out var kb))
            {
                Vector3 pushDirection = (other.transform.position - transform.position).normalized;
                kb.ApplyKnockback(pushDirection, _knockbackForce);
            }
        }
    }
}