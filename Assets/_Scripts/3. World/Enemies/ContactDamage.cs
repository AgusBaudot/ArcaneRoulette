using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class ContactDamage : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<IDamageable>(out var damageable))
                return;
            
            DamageSystem.Deal(damageable, (damageable as Component)?.gameObject, Helpers.Combat.BaseContactDamage, ElementType.Neutral, DamageJuice.Heavy);
        }
    }
}