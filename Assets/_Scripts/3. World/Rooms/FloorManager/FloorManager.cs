using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(EncounterGenerator))]
    public class FloorManager : MonoBehaviour
    {
        [Header("Components")]
        private MapGenerator _mapGenerator;
        private MapSpawner _mapSpawner;
        private EncounterGenerator _encounterGenerator;

        [Header("Data")]
        [SerializeField] private MapGeneratorData _generatorData;
        [SerializeField] private MapSpawnerData _spawnerData;

        [Header("MoveBetweenRooms")]
        private RoomManager _currentRoom;
        [SerializeField] private PlayerController _player;

        [Tooltip("Don't assign it anything, it shouldn't break, but it won't do anything.")]
        [Header("RunInfo")] // info solo para leer desde el inspector
        [SerializeField] private int _roomsVisited;
        [SerializeField] private int LobbyIndex;

        public static FloorManager instance;

        private void Awake()
        {
            _mapGenerator = new MapGenerator();
            _mapSpawner = new MapSpawner();
            _encounterGenerator = GetComponent<EncounterGenerator>();
            _mapGenerator.Init(_generatorData);
            _mapSpawner.Init(_spawnerData);

            if (_player == null)
            {
                Debug.Log($"<color=red>FAIL:</color> player reference is =  {_player == null}");
            }
        }
        private void Start()
        {
            instance = this;
            StartCoroutine(GenerateFloor());
        }
        private IEnumerator GenerateFloor()
        {
            var (rooms, success) = _mapGenerator.SetupDungeon();

            if (!success)
            {
                Debug.Log("MapGeneration Fail");
                yield break;
            }

            yield return null;

            _mapSpawner.InstantiateRooms(rooms, _mapGenerator.getFloorPlan);
            LobbyIndex = _mapGenerator.LobbyRoomIndex;

            yield return null;
            StartRun(EdgeDirection.Up);
        }
        private void StartRun(EdgeDirection dir)
        {
            if (_mapSpawner.RoomLookup.TryGetValue(_mapGenerator.LobbyRoomIndex, out RoomManager room))
            {
                _currentRoom = room;
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
            int playerDirection = 0;
            switch (dir)
            {
                case EdgeDirection.Up:
                    playerDirection = -DungeonGrid.GRID_WIDTH;
                    break;
                case EdgeDirection.Down:
                    playerDirection = DungeonGrid.GRID_WIDTH;
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
