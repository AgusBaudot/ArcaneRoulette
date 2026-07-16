using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(RoomConnections))]
    [RequireComponent(typeof(EntityController))]
    public class RoomManager : MonoBehaviour
    {
        [Header("Room Data")]
        [SerializeField] private int _index;
        [SerializeField] private RoomType _roomType;
        [SerializeField] private RoomState _state;
        [SerializeField] private bool _cleared = false;

        [Header("Portal Spawning")]
        [Tooltip("Boss rooms only. Should have Portal.cs configured as PortalType.NextFloor.")]
        [SerializeField] private GameObject _portalPrefab;
        [Tooltip("Where to spawn the portal. If null, uses the room's transform.position.")]
        [SerializeField] private Transform _portalSpawnPoint;

        public int Index => _index;
        public RoomType Type => _roomType;
        public bool Cleared => _cleared;

        private RoomConnections _roomConnections;
        private EntityController _entityController;
        public RoomConnections GetRoomConnections => _roomConnections;

        public void Awake()
        {
            _roomConnections = GetComponent<RoomConnections>();
            _entityController = GetComponent<EntityController>();
        }

        public void Init(RoomInfo info)
        {
            _index = info.index;
            _roomType = info.roomType;
            _state = RoomState.Idle;
        }

        public void InitDoors(AllDoorsInfo info)
        {
            _roomConnections.SetDoorColors(info);
            _roomConnections.CalculateSpawnsEntry();
        }

        public void InitEntity(RoomEncounterData data)
        {
            _entityController.SaveEnemiesData(data);
        }

        public void EnableRoom()
        {
            _roomConnections.OnDoorActivated -= HandleDoorTransition;
            _roomConnections.OnDoorActivated += HandleDoorTransition;

            EventBus.Publish(new PlayerEnteredRoomEvent(_index));

            if (!_cleared)
            {
                switch (_roomType)
                {
                    case RoomType.Combat:
                    case RoomType.Boss:
                        _entityController.RoomIsClear -= RoomClearedEvent;
                        _entityController.RoomIsClear += RoomClearedEvent;
                        _entityController.PlayEntityController();
                        break;

                    case RoomType.Start:
                        _cleared = true;
                        _state = RoomState.Cleared;
                        _roomConnections.RoomCleared();
                        break;

                    default:
                        // Resting, Artifact, Shop, Portal: none of these lock doors — always
                        // open immediately. They differ in what happens AFTER: Resting/Artifact
                        // become cleared when the player claims their reward (whatever triggers
                        // that calls MarkAsCleared()). Shop and Portal never call MarkAsCleared
                        // at all — per the design doc, they can never clear.
                        _roomConnections.RoomCleared();
                        break;
                }
            }

            _roomConnections.EnableConnections();

            if (_state == RoomState.Idle)
                _state = RoomState.Active;
        }

        public void DisableRoom()
        {
            _roomConnections.OnDoorActivated -= HandleDoorTransition;
            _entityController.RoomIsClear -= RoomClearedEvent;
            _roomConnections.DisableConnections();
        }

        private void RoomClearedEvent()
        {
            _entityController.RoomIsClear -= RoomClearedEvent;
            _cleared = true;
            _state = RoomState.Cleared;

            // Combat/Boss doors were actually locked at entry — this is the real unlock.
            // Everything else that can reach this method (Resting/Artifact, via
            // MarkAsCleared) already had its doors opened back in EnableRoom(), so
            // re-calling RoomCleared() here would just be a redundant no-op lift.
            if (_roomType == RoomType.Combat || _roomType == RoomType.Boss)
            {
                _entityController.DisableAllHazards();
                _roomConnections.RoomCleared();
            }

            switch (_roomType)
            {
                case RoomType.Boss:
                    SpawnPortal();
                    break;

                case RoomType.Resting:
                case RoomType.Artifact:
                    EventBus.Publish(new PassiveRoomClearEvent(_index));
                    break;

                default: // Combat
                    EventBus.Publish(new RoomClearEvent(_index));
                    break;
            }
        }

        private void SpawnPortal()
        {
            if (_portalPrefab == null)
            {
                Debug.LogError($"RoomManager (Index {_index}, {_roomType}): _portalPrefab not assigned.");
                return;
            }
            Vector3 spawnPos = _portalSpawnPoint != null ? _portalSpawnPoint.position : transform.position;
            Instantiate(_portalPrefab, spawnPos, Quaternion.identity);
        }

        private void HandleDoorTransition(EdgeDirection direction)
        {
            // Was FloorManager.instance.TeleportPlayer(direction, Index) — the static
            // singleton your doc already flags. FloorSpawner owns the Index -> RoomManager
            // map from spawning, so it resolves this instead.
            EventBus.Publish(new RoomTransitionRequestEvent(Index, direction));
        }

        public void MarkAsCleared()
        {
            if (_cleared) return;
            RoomClearedEvent();
        }
    }
}