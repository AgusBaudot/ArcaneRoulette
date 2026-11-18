using Foundation;
using UnityEngine;
using World;

namespace Core
{
    public class RoomRewardSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RewardChest _chestPrefab;

        private RewardChest _chest;
        private int _chestRoomIndex = -1;

        private void OnEnable()
        {
            EventBus.Subscribe<RoomClearEvent>(HandleRoomClear);
            EventBus.Subscribe<PlayerEnteredRoomEvent>(HandlePlayerEnteredRoom);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoomClearEvent>(HandleRoomClear);
            EventBus.Unsubscribe<PlayerEnteredRoomEvent>(HandlePlayerEnteredRoom);
        }

        private void HandleRoomClear(RoomClearEvent evt)
        {
            if (_chest != null)
            {
                Destroy(_chest.gameObject);
                _chest = null;
            }

            _chestRoomIndex = evt.Index;
            _chest = Instantiate(_chestPrefab, evt.Position + Vector3.up * 0.5f, _chestPrefab.transform.rotation);
        }

        private void HandlePlayerEnteredRoom(PlayerEnteredRoomEvent evt)
        {
            if (_chest != null && evt.Index != _chestRoomIndex)
            {
                Destroy(_chest.gameObject);
                _chest = null;
                _chestRoomIndex = -1;
            }
        }
    }
}