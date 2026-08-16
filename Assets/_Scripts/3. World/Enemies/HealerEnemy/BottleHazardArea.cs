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
                ApplyPuddleDebuffs(((Component)enemyReceiver).gameObject);
            }
            else if (!_affectsEnemies)
            {
                var player = other.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    ApplyPuddleDebuffs(player.gameObject);
                }
            }
        }

        private void ApplyPuddleDebuffs(GameObject rootObj)
        {
            var debuffComp = rootObj.GetComponent<DebuffComponent>();
            if (debuffComp == null)
            {
                debuffComp = rootObj.AddComponent<DebuffComponent>();
            }

            debuffComp.ApplyDebuff(DebuffType.Speed, 0.40f, 3f, _sourceId);
            debuffComp.ApplyDebuff(DebuffType.ATK, 0.20f, 3f, _sourceId);
        }
    }
}