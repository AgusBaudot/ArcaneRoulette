using System;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(EnemyHealth))]
    public abstract class BaseEnemy : MonoBehaviour
    {
        protected Transform _playerTarget;
        public event Action<BaseEnemy> OnDeath;

        protected virtual void Awake()
        {
            var health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath += HandleHealthDeath;
            }
        }

        protected void OnDestroy()
        {
            var health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath -= HandleHealthDeath;
            }
        }

        private void HandleHealthDeath()
        {
            OnDeath?.Invoke(this);
            
            Destroy(gameObject);
        }

        public void Init(Transform playerTarget)
        {
            _playerTarget = playerTarget;
        }
    }
}