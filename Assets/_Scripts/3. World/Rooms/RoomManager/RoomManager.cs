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
        [Tooltip("The portal prefab to spawn when this room is cleared (Boss/Portal rooms only).")]
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
                if (_roomType == RoomType.Combat || _roomType == RoomType.Boss)
                {
                    _entityController.RoomIsClear -= RoomClearedEvent;
                    _entityController.RoomIsClear += RoomClearedEvent;
                    _entityController.PlayEntityController();
                }
                else if (_roomType == RoomType.Resting)
                {
                    _roomConnections.RoomCleared();
                }
                else if (_roomType == RoomType.Portal)
                {
                    _roomConnections.RoomCleared();
                }
                else
                {
                    _cleared = true;
                    _state = RoomState.Cleared;
                    _roomConnections.RoomCleared();
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
            _entityController.DisableAllHazards();
            _cleared = true;
            _state = RoomState.Cleared;
    
            if (_roomType != RoomType.Resting)
            {
                _roomConnections.RoomCleared();
            }
    
            if (_roomType == RoomType.Boss)
            {
                // EventBus.Publish(new EndFloorClearEvent(_index));
                //This should spawn the portal instead.
            }
            else if (_roomType == RoomType.Resting)
            {
                EventBus.Publish(new PassiveRoomClearEvent(_index));
            }
            else
            {
                EventBus.Publish(new RoomClearEvent(_index));
            }
        }

        private void HandleDoorTransition(EdgeDirection direction)
        {
            FloorManager.instance.TeleportPlayer(direction, Index);
        }

        public void MarkAsCleared()
        {
            if (_cleared) return;
            RoomClearedEvent();
        }
    }
}