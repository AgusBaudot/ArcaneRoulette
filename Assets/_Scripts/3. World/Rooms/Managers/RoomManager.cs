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
        [SerializeField] private int _index;
        [SerializeField] private int _value;
        [SerializeField] private RoomType _roomType;
        [SerializeField] private RoomState _state;

        public int Index => _index;
        public int Value => _value;
        public RoomType Type => _roomType;

        private RoomConnections _roomConnections;
        private EntityController _entityController;

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
        }
        public void EnableRoom() 
        {
            _roomConnections.EnableConnections();
        }
        public void Start()
        {
            _roomConnections = GetComponent<RoomConnections>();
            _entityController = GetComponent<EntityController>();

            _roomConnections.OnDoorActivated += HandleDoorTransition;
            _entityController.RoomIsClear += FireEventBusEvent;
        }

        private void FireEventBusEvent() 
        {
            _state = RoomState.Cleared;
            EventBus.Publish(new RoomClearEvent { roomId = _index });
        }
        private void HandleDoorTransition(EdgeDirection direction)
        {
            _roomConnections.DisableConnections();
            //FloorManager.Instance.Cambio_de_Room(direction, this);
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