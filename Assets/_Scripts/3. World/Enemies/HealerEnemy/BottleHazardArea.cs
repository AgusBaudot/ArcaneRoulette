using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class BottleHazardArea : MonoBehaviour
    {
        [SerializeField] private float _duration = 10f;
        [SerializeField] private float _tickRate = 0.5f;
        
        private bool _affectsEnemies;
        private float _timer;
        private string _sourceId;

        public void InitHazard(bool affectsEnemies)
        {
            _affectsEnemies = affectsEnemies;
            _sourceId = "Environment/Bottle_" + GetInstanceID();
            Destroy(gameObject, _duration);
        }

        private void OnTriggerStay(Collider other)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _timer = _tickRate;

            if (_affectsEnemies && other.TryGetComponent<IDebuffReceiver>(out var enemyReceiver))
            {
                ApplyPuddleDebuffs(other.gameObject);
            }
            else if (!_affectsEnemies && other.TryGetComponent<PlayerController>(out _))
            {
                ApplyPuddleDebuffs(other.gameObject);
            }
        }

        private void ApplyPuddleDebuffs(GameObject target)
        {
            if (target.TryGetComponent<DebuffComponent>(out var debuffComp))
            {
                // Refresh interval logic per standard FDD (AoE lasts 10s, debuffs stick for 3s after leaving)
                debuffComp.ApplyDebuff(DebuffType.Speed, 0.40f, 3f, _sourceId);
                debuffComp.ApplyDebuff(DebuffType.ATK, 0.20f, 3f, _sourceId);
            }
        }
    }
}