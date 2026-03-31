using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] private int _amount = 10;
        [SerializeField] private ElementType _element = ElementType.Neutral;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<IDamageable>(out var damageable))
                return;
            
            damageable.TakeDamage(_amount, _element);
        }
    }
}