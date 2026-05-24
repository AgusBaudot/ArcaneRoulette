using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace World 
{
    [RequireComponent(typeof(MapGenerator))]
    [RequireComponent(typeof(MapSpawner))]
    [RequireComponent (typeof(EncounterGenerator))]
    public class FloorManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private RoomDoor _startRoom;
        [SerializeField] private RoomDoor _generateFloor;

        [Header("MapMaker")]
        private MapGenerator _mapGenerator;
        private MapSpawner _mapSpawner;
        private EncounterGenerator _encounterGenerator;

        [Header("MoveBetweenRooms")]
        private RoomManager _currentRoom;
        [SerializeField] private PlayerController _player;

        [Header("RunInfo")]
        [SerializeField] private int _roomsVisited = 0;

        public int EndOfTheFloor;
        private int _currentIndex; // todavia no se usa pero por las dudas se guarda
        
        
        public static FloorManager instance;
        private void Awake()
        {
            _mapGenerator = GetComponent<MapGenerator>();
            _mapSpawner = GetComponent<MapSpawner>();
            _encounterGenerator = GetComponent<EncounterGenerator>();

            _mapGenerator.Init();
            _mapSpawner.Init();

            EndOfTheFloor = _mapGenerator.BossRoomIndex;
        }
        private void Start()
        {
            instance = this;
            //_player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>(); Esto no se por que no funciona
            _startRoom.OnPlayerEnter += StartRun;
            _generateFloor.OnPlayerEnter += GenerateFloor;
        }
        private void GenerateFloor(EdgeDirection dir) 
        {
            _generateFloor.OnPlayerEnter -= GenerateFloor;

            List<RoomInfo> rooms = _mapGenerator.SetupDungeon();

            _mapSpawner.SetUpRooms(rooms, _mapGenerator.getFlorrPlan);
        }
        private void StartRun(EdgeDirection dir)
        {
            _startRoom.OnPlayerEnter -= StartRun;
            if (_mapSpawner.RoomLookup.TryGetValue(45, out RoomManager room))
            {
                _currentRoom = room;
                _currentIndex = 45;
                room.gameObject.SetActive(true);
                SetupRoomEncounter(room);
                room.EnableRoom();
                // Reemplazo
                _player.SetCanMove(false);
                _player.Rb.velocity = Vector3.zero;
                _player.transform.position = room.GetRoomConnections.GetPlayerSpawn(dir);
                _player.SetCanMove(true);
                // hasta aca
            }
        }
        public void TeleportPlayer(EdgeDirection dir, int currentIndexRoom)
        { 
            _currentIndex = currentIndexRoom;
            int playerDirection = 0;
            switch (dir) 
            {
                case EdgeDirection.Up:
                    playerDirection = -10;
                    break;
                case EdgeDirection.Down:
                    playerDirection = 10;
                    break;
                case EdgeDirection.Left:
                    playerDirection = -1;
                    break;
                case EdgeDirection.Right:
                    playerDirection = 1;
                    break;
            }

            if (_mapSpawner.RoomLookup.TryGetValue(currentIndexRoom + playerDirection, out RoomManager room)) 
            {
                _currentRoom.DisableRoom();
                _currentRoom.gameObject.SetActive(false);
                _currentRoom = room;
                _currentRoom.gameObject.SetActive(true);
                // cambiar esto
                _player.SetCanMove(false);
                _player.Rb.velocity = Vector3.zero;
                _player.Rb.position = room.GetRoomConnections.GetPlayerSpawn(dir);
                _player.SetCanMove(true);
                // hasta aca, por un metodo del player
                SetupRoomEncounter(room);
                _currentRoom.EnableRoom();
            }
        }
        private void SetupRoomEncounter(RoomManager room)
        {
            if (room.Cleared) return;
            _roomsVisited++;
            RoomEncounterData data = _encounterGenerator.Generate(room.Type, _roomsVisited);
            room.InitEntity(data);
        }
    }
}
