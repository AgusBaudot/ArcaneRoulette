using Foundation;
using UnityEngine;

namespace World
{
    public class RoomSpawner : MonoBehaviour
    {
        [SerializeField] private int _roomId;
        [SerializeField] private PickupDropPool _dropPool;
        [SerializeField] private int _pickupCount = 3;
        [SerializeField] private float _dropRadius = 1.5f;

        private void OnEnable()
        {
            EventBus.Subscribe<RoomManager.RoomClearEvent>(OnRoomCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoomManager.RoomClearEvent>(OnRoomCleared);
        }

        private void OnRoomCleared(RoomManager.RoomClearEvent evt)
        {
            if (evt.roomId != _roomId)
                return;

            Debug.Log("RoomSpawner: room " + evt.roomId + " cleared.");
            HandleRoomCleared();
        }

        private void HandleRoomCleared()
        {
            if (_dropPool == null)
            {
                Debug.LogWarning($"RoomSpawner {_roomId}: No drop pool assigned.");
                return;
            }

            for (int i = 0; i < _pickupCount; i++)
            {
                GameObject prefab = _dropPool.GetRandomPickupPrefab();
                if (prefab == null)
                    continue;

                Vector3 dropPosition = transform.position + GetDropOffset(i);
                Instantiate(prefab, dropPosition, prefab.transform.rotation);
            }
        }

        private Vector3 GetDropOffset(int index)
        {
            float angle = index * Mathf.PI * 2f / Mathf.Max(1, _pickupCount);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _dropRadius;
        }
    }
}
