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

            EventBus.Publish(new PlayerEnteredRoomEvent(_index));

            if (!_cleared)
            {
                switch (_roomType)
                {
                    case RoomType.Combat:
                        _roomConnections.LockDoors();
                        _entityController.RoomIsClear -= RoomClearedEvent;
                        _entityController.RoomIsClear += RoomClearedEvent;
                        _entityController.PlayEntityController();
                        break;
                    
                    case RoomType.Boss:
                        _roomConnections.LockDoors();
                        _entityController.RoomIsClear -= RoomClearedEvent;
                        _entityController.RoomIsClear += RoomClearedEvent;
                        _entityController.PlayEntityController();
                        //EventBus.Publish(new EndFloorClearEvent(_index));
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
                // If the player backtracks into an already cleared room, 
                // ensure the doors animate open/stay open.
                _roomConnections.RoomCleared();
            }

            _roomConnections.EnableConnections();

            if (_state == RoomState.Idle)
                _state = RoomState.Active;

            //THIS SOMEHOW SPAWNS A PORTAL IN THE INITIAL ROOM WHEN PORTAL ROOM IS CLEARED
            //if (_roomType == RoomType.Portal)
            //    SpawnPortal();
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
                    EventBus.Publish(new RoomClearEvent(_index));
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