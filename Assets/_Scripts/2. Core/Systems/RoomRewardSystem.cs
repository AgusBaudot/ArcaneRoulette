using Foundation;
using UnityEngine;
using World;

namespace Core
{
    public class RoomRewardSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RewardChest _chestPrefab;
        
        [Header("Economy Tuning")]
        [SerializeField] private int _pickupReward = 1;
        [SerializeField] private int _standardRoomReward = 10;
        [SerializeField] private int _bossRoomReward = 50;
        [SerializeField] private AudioEventSO _currencyEarnedSound;

        private RewardChest _chest;

        private void OnEnable()
        {
            EventBus.Subscribe<LootChestOpenedEvent>(HandleChestOpen);
            EventBus.Subscribe<RoomClearEvent>(HandleRoomClear);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LootChestOpenedEvent>(HandleChestOpen);
            EventBus.Unsubscribe<RoomClearEvent>(HandleRoomClear);
        }

        private void HandleRoomClear(RoomClearEvent evt)
        {
            _chest = Instantiate(_chestPrefab, evt.Position + Vector3.up * 0.5f, Quaternion.identity);
        }

        private void HandleChestOpen(LootChestOpenedEvent evt)
        {
            GameStateManager.RunState.AddCurrency(10);

            if (_currencyEarnedSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _currencyEarnedSound });
            }
            
            //TODO: Animation of opening.
            Destroy(_chest.gameObject, 1f);
            //TODO: How does the chest disappear?
        }
    }
}