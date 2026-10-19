using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class ContactDamage : MonoBehaviour
    {
        private readonly Dictionary<Collider, float> _cooldowns = new();

        private void OnTriggerEnter(Collider other)
        {
            TryDealDamage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryDealDamage(other);
        }

        private void OnTriggerExit(Collider other)
        {
            _cooldowns.Remove(other);
        }

        private void OnDisable()
        {
            _cooldowns.Clear();
        }

        private void TryDealDamage(Collider other)
        {
            if (_cooldowns.TryGetValue(other, out float nextDamageTime) && Time.time < nextDamageTime)
                return;

            if (!other.TryGetComponent<IDamageable>(out var damageable))
                return;

            var batch = new DamageBatch();
            batch.Deal(damageable, Helpers.Combat.BaseContactDamage, ElementType.Neutral);
            batch.Commit(Helpers.Combat.PlayerDamage);

            _cooldowns[other] = Time.time + Helpers.Combat.ContactDamageInterval;
        }
    }
}