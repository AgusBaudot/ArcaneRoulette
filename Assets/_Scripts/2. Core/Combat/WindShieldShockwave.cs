using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    public class WindShieldShockwave : MonoBehaviour, IUpdatable
    {
        //IUpdatable
        public int UpdatePriority => Foundation.UpdatePriority.Spells;

        [SerializeField] private float _knockbackForce;

        private float _shieldLifeTime;
        private SphereCollider _collider;
        private HashSet<IDamageable> _enemiesHit = new();

        private void Start()
        {
            _collider = GetComponent<SphereCollider>();
            Destroy(gameObject, 1f);
        }
        
        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance.Unregister(this);
        }

        public void Tick(float dt)
        {
            _shieldLifeTime += dt;
            float percent = _shieldLifeTime / 0.6f; //0.6 is the total duration of the VFX
            _collider.radius = Mathf.Lerp(0, 5, percent);
        }

        private void OnTriggerEnter(Collider other)
        {
            //Filter out the player immediately to save processing
            if (other.CompareTag("Player"))
                return;
            
            //Check if damageable
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                //Check if specific enemy was already hit
                if (!_enemiesHit.Contains(damageable))
                {
                    Debug.Log("Wire it with windshield to receive damage info");
                    
                    _enemiesHit.Add(damageable);
                    // damageable.TakeDamage(2, ElementType.Wind);
                    
                    Debug.Log("Might want to check separately to see if knockbackable is not damageable");
                    if (other.TryGetComponent<IKnockbackable>(out var kb))
                    {
                        Vector3 pushDirection = (other.transform.position - transform.position).normalized;
                        kb.ApplyKnockback(pushDirection, _knockbackForce);
                    }
                }
            }

        }
    }
}