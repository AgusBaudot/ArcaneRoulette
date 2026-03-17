using System;
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
            //Avoid misconfiguration in the prefab
            gameObject.layer = LayerMask.NameToLayer("PlayerHurtBox");
            GetComponent<Collider>().isTrigger = true;
        }

        public void Initialize(PlayerHealth health) => _health = health;


        public void TakeDamage(int amount, ElementType elementType)
        {
            _health.TakeDamage(amount, elementType);
        }
    }
}