using Foundation;
using UnityEngine;

namespace World
{
    public sealed class StatueOfGreed : RestingStatue
    {
        [Header("Rewards")] [SerializeField] private PickupDropPool _dropPool;
        [SerializeField] private AudioEventSO _manaCrystalSound;
        
        protected override void ApplyReward(GameObject player)
        {
            GameStateManager.RunState.AddCurrency(15);

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
        }
    }
}