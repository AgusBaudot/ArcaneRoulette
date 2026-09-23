using Foundation;
using UnityEngine;
using World;

namespace Core
{
    public class RoomRewardSystem : MonoBehaviour
    {
        [Header("Economy Tuning")]
        [SerializeField] private int _pickupReward = 1;
        [SerializeField] private int _standardRoomReward = 10;
        [SerializeField] private int _bossRoomReward = 50;
        [SerializeField] private AudioEventSO _currencyEarnedSound;

        private void OnEnable()
        {
            EventBus.Subscribe<RoomClearEvent>(OnRoomCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoomClearEvent>(OnRoomCleared);
        }

        private void OnRoomCleared(RoomClearEvent evt)
        {
            // int rewardAmount = evt.IsBossRoom ? _bossRoomReward : _standardRoomReward;

            GameStateManager.RunState.AddCurrency(10);

            if (_currencyEarnedSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _currencyEarnedSound });
            }
        }
    }
}