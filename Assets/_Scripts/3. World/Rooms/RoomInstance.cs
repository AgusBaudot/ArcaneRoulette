using System.Collections.Generic;
using Core;
using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(BoxCollider))] // room-bounds trigger; isTrigger set on the prefab
    public sealed class RoomInstance : MonoBehaviour
    {
        [Header("Door Sockets — leave a slot null if this prefab has no door in that direction")]
        [SerializeField] private RoomDoorSocket _up;
        [SerializeField] private RoomDoorSocket _down;
        [SerializeField] private RoomDoorSocket _left;
        [SerializeField] private RoomDoorSocket _right;

        public int Index { get; private set; }
        public RoomType Type { get; private set; }
        public bool IsCleared { get; private set; }

        private readonly List<RoomDoorSocket> _openSockets = new List<RoomDoorSocket>(4);
        private bool _hasPlayerEntered;

        public void Configure(int index, RoomType type, IReadOnlyDictionary<Vector2Int, bool> openDirections)
        {
            Index = index;
            Type = type;
            _openSockets.Clear();

            TryConfigureSocket(_up, Vector2Int.up, openDirections);
            TryConfigureSocket(_down, Vector2Int.down, openDirections);
            TryConfigureSocket(_left, Vector2Int.left, openDirections);
            TryConfigureSocket(_right, Vector2Int.right, openDirections);
        }

        private void TryConfigureSocket(RoomDoorSocket socket, Vector2Int direction,
            IReadOnlyDictionary<Vector2Int, bool> openDirections)
        {
            if (socket == null) return;
            bool isOpen = openDirections.TryGetValue(direction, out bool open) && open;
            socket.SetOpen(isOpen);
            if (isOpen) _openSockets.Add(socket);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerHurtBox>(out _)) return;

            if (!_hasPlayerEntered)
            {
                _hasPlayerEntered = true;
                EventBus.Publish(new PlayerEnteredRoomEvent(Index));
            }

            // Design doc: Combat Room doors lock the instant the player enters.
            if (Type == RoomType.Combat && !IsCleared)
                SetDoorsLocked(true);
        }

        /// <summary>
        /// Call once this room's actual objective is done — last wave dead, boss dead,
        /// a statue interacted with. Shop and Portal never call this; they can never clear.
        /// </summary>
        public void MarkCleared()
        {
            if (IsCleared) return;
            IsCleared = true;

            switch (Type)
            {
                case RoomType.Combat:
                    SetDoorsLocked(false);
                    EventBus.Publish(new RoomClearEvent(Index));
                    break;
                case RoomType.Boss:
                    EventBus.Publish(new EndFloorClearEvent(Index));
                    break;
                case RoomType.Resting:
                case RoomType.Artifact:
                    EventBus.Publish(new PassiveRoomClearEvent(Index));
                    break;
                default:
                    Debug.LogWarning($"RoomInstance: MarkCleared() called on a {Type} room " +
                                      $"(Index {Index}) — the design doc says these can never be cleared.");
                    break;
            }
        }

        private void SetDoorsLocked(bool locked)
        {
            foreach (var socket in _openSockets)
                socket.SetLocked(locked);
        }
    }
}