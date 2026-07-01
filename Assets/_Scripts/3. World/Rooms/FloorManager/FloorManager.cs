using System.Collections;
using System.Collections.Generic;
using Core;
using Foundation;
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
            instance = this;
            _mapGenerator = new MapGenerator(_generatorData);
            _mapSpawner = new MapSpawner();
            _encounterGenerator = GetComponent<EncounterGenerator>();
            _mapSpawner.Init(_spawnerData);

            if (_player == null)
            {
                Debug.Log($"<color=red>FAIL:</color> player reference is =  {_player == null}");
            }
        }
        private void Start()
        {
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

            BuildAndSendMapStateToFoundation();
            
            StartRun(EdgeDirection.Up);
        }

        private void BuildAndSendMapStateToFoundation()
        {
            Dictionary<int, VolatileRunState.RoomMapData> layout = new();
            int[] plan = _mapGenerator.getFloorPlan;

            foreach (var kvp in _mapSpawner.RoomLookup)
            {
                int idx = kvp.Key;
                RoomManager room = kvp.Value;

                List<int> neighbors = new();
        
                int upIndex = idx - DungeonGrid.GRID_WIDTH;
                if (upIndex >= 0 && plan[upIndex] != 0) neighbors.Add(upIndex);
        
                int downIndex = idx + DungeonGrid.GRID_WIDTH;
                if (downIndex < plan.Length && plan[downIndex] != 0) neighbors.Add(downIndex);
        
                int x = DungeonGrid.GetX(idx);
                if (x > 0 && plan[idx - 1] != 0) neighbors.Add(idx - 1);
                if (x < DungeonGrid.GRID_WIDTH - 1 && plan[idx + 1] != 0) neighbors.Add(idx + 1);

                layout[idx] = new VolatileRunState.RoomMapData
                {
                    Index = idx,
                    X = x,
                    Y = DungeonGrid.GetY(idx),
                    Type = room.Type,
                    IsCleared = room.Cleared,
                    IsDiscovered = false,
                    NeighborIndices = neighbors.ToArray()
                };
            }

            GameStateManager.RunState.InitializeFloorMap(layout);
        }
        
        private void StartRun(EdgeDirection dir)
        {
            if (_mapSpawner.RoomLookup.TryGetValue(_mapGenerator.LobbyRoomIndex, out RoomManager room))
            {
                _currentRoom = room;
                room.gameObject.SetActive(true);
                SetupRoomEncounter(room);
                room.EnableRoom();
                _player.TeleportTo(room.GetRoomConnections.GetPlayerSpawn(dir));
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

                _player.TeleportTo(room.GetRoomConnections.GetPlayerSpawn(dir));

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
