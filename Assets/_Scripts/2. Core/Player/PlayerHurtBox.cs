using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public class PlayerHurtBox : MonoBehaviour, IDamageable
    {
        private PlayerHealth _health;

        private void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer("PlayerHurtBox");
            GetComponent<Collider>().isTrigger = true;
        }

        public void Initialize(PlayerHealth health) => _health = health;


        public bool TakeDamage(int amount, ElementType elementType)
        {   
            return _health.TakeDamage(amount, elementType);
        }
    }
}