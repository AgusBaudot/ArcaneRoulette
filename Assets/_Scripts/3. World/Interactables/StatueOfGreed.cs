using System.Collections;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class StatueOfGreed : RestingStatue
    {
        [Header("Rewards")] 
        [SerializeField] private PickupDropPool _dropPool;
        [SerializeField] private AudioEventSO _manaCrystalSound;
        
        [Header("Currency Settings")]
        [SerializeField] private int _manaCrystalAmount = 5;
        [SerializeField] private float _currencySpawnDelay = 0.15f;
        
        protected override void ApplyReward(GameObject player)
        {
            if (_manaCrystalSound != null)
            {
                EventBus.Publish(new AudioPlayRequest
                { 
                    Event = _manaCrystalSound, 
                    WorldPosition = transform.position 
                });
            }

            if (_dropPool != null)
            {
                var runes = _dropPool.GetRandomRunes(1);
                if (runes != null && runes.Length > 0)
                {
                    GameStateManager.RunState.AddRune(runes[0]);
                }
            }
            
            //TODO: fire Animation for Rune acquired here.

            StartCoroutine(SpawnCurrencyRoutine());
        }

        private IEnumerator SpawnCurrencyRoutine()
        {
            WaitForSeconds wait = CoroutineUtils.GetWait(_currencySpawnDelay);
            
            for (int i = 0; i < _manaCrystalAmount; i++)
            {
                EventBus.Publish(new CurrencyDropEvent(transform.position));
                
                yield return wait;
            }
        }
    }
}