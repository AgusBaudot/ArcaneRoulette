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
            
            var batch = new DamageBatch();
            batch.Deal(damageable, Helpers.Combat.BaseContactDamage, ElementType.Neutral);
            batch.Commit(Helpers.Combat.PlayerDamage);
        }
    }
}