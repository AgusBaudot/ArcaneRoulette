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
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _doorCloseSound;
        [SerializeField] private AudioEventSO _doorOpenSound;

        public int Index => _index;
        public RoomType Type => _roomType;
        public bool Cleared => _cleared;

        private RoomConnections _roomConnections;
        private EntityController _entityController;
        private bool _waitingForChest;
        
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
            _roomConnections.InitializeDeadEnds();
        }

        public void InitEntity(RoomEncounterData data)
        {
            _entityController.SaveEnemiesData(data);
        }

        public void EnableRoom()
        {
            _roomConnections.OnDoorActivated -= HandleDoorTransition;
            _roomConnections.OnDoorActivated += HandleDoorTransition;
            
            EventBus.Subscribe<LootChestOpenedEvent>(HandleChestOpened);

            EventBus.Publish(new PlayerEnteredRoomEvent(_index));

            if (!_cleared)
            {
                switch (_roomType)
                {
                    case RoomType.Combat:
                    case RoomType.Boss:
                        _roomConnections.LockDoors();
                        if (_doorCloseSound != null)
                        {
                            EventBus.Publish(new AudioPlayRequest {Event =  _doorCloseSound, WorldPosition = transform.position});
                        }
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
                        // Resting, Artifact, Shop, Portal
                        _roomConnections.RoomCleared();
                        break;
                }
            }
            else 
            {
                _roomConnections.RoomCleared();
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
            
            EventBus.Unsubscribe<LootChestOpenedEvent>(HandleChestOpened);
        }

        private void RoomClearedEvent()
        {
            _entityController.RoomIsClear -= RoomClearedEvent;

            if (_roomType == RoomType.Combat || _roomType == RoomType.Boss)
            {
                _entityController.DisableAllHazards();
            }

            switch (_roomType)
            {
                case RoomType.Boss:
                case RoomType.Combat:
                    _waitingForChest = true;
                    EventBus.Publish(new RoomClearEvent(_index, transform.position));
                    break;

                case RoomType.Resting:
                case RoomType.Artifact:
                    // Non-combat rooms clear instantly
                    _cleared = true;
                    _state = RoomState.Cleared;
                    EventBus.Publish(new PassiveRoomClearEvent(_index));
                    break;
            }
        }

        private void HandleChestOpened(LootChestOpenedEvent evt)
        {
            if (!_waitingForChest) 
                return;
            
            _waitingForChest = false;
            
            _cleared = true;
            _state = RoomState.Cleared;

            _roomConnections.RoomCleared();

            if (_doorOpenSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _doorOpenSound, WorldPosition = transform.position });
            }

            if (_roomType == RoomType.Boss)
            {
                SpawnPortal();
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
            Instantiate(_portalPrefab, spawnPos, _portalPrefab.transform.rotation);
        }

        private void HandleDoorTransition(EdgeDirection direction)
        {
            EventBus.Publish(new RoomTransitionRequestEvent(Index, direction));
        }

        public void MarkAsCleared()
        {
            if (_cleared) 
                return;
            
            RoomClearedEvent();
        }
    }
}