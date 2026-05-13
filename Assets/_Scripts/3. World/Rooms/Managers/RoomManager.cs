using Foundation;
using UnityEngine;

namespace World 
{
    public enum RoomState
    {
        Idle,
        Active,
        Cleared,
        Reward,
        Unlocked
    }

    [RequireComponent(typeof(RoomConnections))]
    [RequireComponent(typeof(EntityController))]
    public class RoomManager : MonoBehaviour
    {
        //Serializado solo para testeo, luego lo saco
        [Header("Room Data")]
        [SerializeField] private int _index;
        [SerializeField] private int _value;
        [SerializeField] private RoomType _roomType;
        [SerializeField] private RoomState _state;
        [SerializeField] private bool _cleared = false;

        //Getters
        public int Index => _index;
        public int Value => _value;
        public RoomType Type => _roomType;

        [Header("ReferenceToMap")]
        private RoomConnections _roomConnections;
        private EntityController _entityController;
        public RoomConnections GetRoomConnections => _roomConnections;

        // ---- Init a Room ----
        public void Awake()
        {
            _roomConnections = GetComponent<RoomConnections>();
            _entityController = GetComponent<EntityController>();   
        }
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                RoomClearedEvent();
            }
        }
        public void Init(RoomInfo info)
        {
            _index = info.index;
            _value = info.value;
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

        // ---- Room Events ----
        public void EnableRoom() 
        {
            _roomConnections.OnDoorActivated -= HandleDoorTransition;
            _roomConnections.OnDoorActivated += HandleDoorTransition;

            if (!_cleared)
            {
                _entityController.RoomIsClear -= RoomClearedEvent;
                _entityController.RoomIsClear += RoomClearedEvent;
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

            _roomConnections.RoomCleared();

            EventBus.Publish(new RoomClearEvent { roomId = _index });
        }
        private void HandleDoorTransition(EdgeDirection direction)
        {
            _roomConnections.DisableConnections();
            Debug.Log("Se Esta llamando a este evento?");
            FloorManager.instance.TeleportPlayer(direction, Index);
        }
        public struct RoomClearEvent
        {
            public int roomId;
            
            //alguna otra info para enviar...
        }
    }
}

//switch (direction)
//{
//    case EdgeDirection.up:
//        Debug.Log("Entro Arriba");
//        break;

//    case EdgeDirection.down:
//        Debug.Log("Entro Abajo");
//        break;

//    case EdgeDirection.left:
//        Debug.Log("Entro Izquierda");
//        break;

//    case EdgeDirection.right:
//        Debug.Log("Entro Derecha");
//        break;

// return RoomManager.instance.doors.FirstOrDefault(x => x.roomType == roomType);