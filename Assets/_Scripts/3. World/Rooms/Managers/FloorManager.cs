using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public enum RunDificult
    {
        Easy,
        Medium,
        Hard
    }
    [RequireComponent(typeof(MapGenerator))]
    [RequireComponent(typeof(MapSpawner))]
    public class FloorManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private RoomDoor _startRoom;
        [SerializeField] private RoomDoor _generateFloor;
        [SerializeField] private RunDificult rundificult;

        [Header("MapMaker")]
        private MapGenerator _mapGenerator;
        private MapSpawner _mapSpawner;

        [Header("MoveBetweenRooms")]
        private GameObject _player;
        private int _currentIndex; // todavia no se usa pero por las dudas se guarda
        private RoomManager _currentRoom;
        public int MaximumRooms = 1;

        public static FloorManager instance;
        private void Awake()
        {
            _mapGenerator = GetComponent<MapGenerator>();
            _mapSpawner = GetComponent<MapSpawner>();
        }
        private void Start()
        {
            instance = this;
            _player = GameObject.FindGameObjectWithTag("Player");
            _startRoom.OnPlayerEnter += StartRun;
            _generateFloor.OnPlayerEnter += GenerateFloor;
        }
        private void GenerateFloor(EdgeDirection dir) 
        {
            _generateFloor.OnPlayerEnter -= GenerateFloor;
            StartCoroutine(StartRunRoutine());
        }
        private IEnumerator StartRunRoutine()
        {
            GenerateFloor();
            yield return new WaitForSeconds(0.1f); // Hay que hacer algo con esto
        }
        private void GenerateFloor()
        {
            List<RoomInfo> rooms = _mapGenerator.SetupDungeon();
            _mapSpawner.SetUpRooms(rooms, _mapGenerator.getFlorrPlan);
            //_mapSpawner.SetUpDoors(_mapGenerator.getFlorrPlan);
        }
        private void StartRun(EdgeDirection dir)
        {
            _startRoom.OnPlayerEnter -= StartRun;
            
            if(_mapSpawner.RoomLookup.TryGetValue(45, out RoomManager room)) // 45 porque es la room inicial donde empieza el MapGenerator linea 57
            {
                _currentRoom = room;
                _currentIndex = 45;
                room.gameObject.SetActive(true);
                room.EnableRoom();
                _player.transform.position = room.GetRoomConnections.GetPlayerSpawn(dir);
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
                _currentRoom.EnableRoom();
                _player.transform.position = room.GetRoomConnections.GetPlayerSpawn(dir);
            }
        }
        private void SetDifficulty()
        {
            /*
            switch (rundificult)
            {
                case RunDificult.easy: extraenemies = 1; break;
                case RunDificult.medium: extraenemies = 2; break;
                case RunDificult.hard: extraenemies = 3; break;
            }
            */
        }
    }
}
