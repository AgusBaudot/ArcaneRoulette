using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class MapSpawner
    {
        [Header("Map Spawner Data")]
        private MapSpawnerData data;

        private Dictionary<RoomType, Material> _doorLookup = new Dictionary<RoomType, Material>();
        private Dictionary<int, RoomManager> _roomLookup = new Dictionary<int, RoomManager>();
        public Dictionary<int, RoomManager> RoomLookup => _roomLookup;

        public void Init(MapSpawnerData mapSpawnerData)
        {
            data = mapSpawnerData;


            foreach (DoorScriptable door in data.doorsMaterials)
            {
                _doorLookup.Add(door.roomType, door.materialDoor);
            }
        }
        public void InstantiateRooms(List<RoomInfo> rooms, int[] floorPlan)
        {
            foreach (RoomManager room in _roomLookup.Values)
            {
                Object.Destroy(room.gameObject);
            }
            _roomLookup.Clear();

            foreach (RoomInfo room in rooms)
            {
                SpawnRoom(room.index, room.roomType, data.roomOffset);
            }

            SetUpDoors(floorPlan);
            SetAllActiveFalse();
        }
        private void SpawnRoom(int index, RoomType roomType, Vector2 offset)
        {
            int x = DungeonGrid.GetX(index);
            int y = DungeonGrid.GetY(index);

            Vector3 position = new Vector3(x * offset.x, 0, -y * offset.y);

            RoomManager prefab = null;

            switch (roomType)
            {
                case RoomType.Combat:
                    int rand = Random.Range(0, data.CombatRoomPrefab.Length);
                    prefab = data.CombatRoomPrefab[rand];
                    break;

                case RoomType.Boss:
                    prefab = data.BossRoomPrefab;
                    break;

                case RoomType.Resting:
                    prefab = data.RestingRoomPrefab;
                    break;

                case RoomType.Portal:
                    prefab = data.PortalRoomPrefab;
                    break;

                case RoomType.Shop:
                    prefab = data.ShopRoomPrefab;
                    break;

                case RoomType.Start:
                    prefab = data.LobbyRoomPrefab;
                    break;

                case RoomType.Artifact:
                    prefab = data.ArtifactRoomPrefab;
                    break;
            }
            RoomManager newRoom = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            //RoomManager newRoom = Instantiate(prefab, position, Quaternion.identity);

            RoomInfo info = new RoomInfo();

            info.index = index;
            info.roomType = roomType;

            newRoom.Init(info);

            _roomLookup.Add(index, newRoom);
        }
        public void SetUpDoors(int[] floorPlan)
        {
            foreach (RoomManager rooms in _roomLookup.Values)
            {
                // save index of neighbors
                int upIndex = rooms.Index - DungeonGrid.GRID_WIDTH;
                int downIndex = rooms.Index + DungeonGrid.GRID_WIDTH;
                int leftIndex = rooms.Index - 1;
                int rightIndex = rooms.Index + 1;

                // por si esta fuera del grid
                int x = DungeonGrid.GetX(rooms.Index);
                bool hasUpBounds = upIndex >= 0;
                bool hasDownBounds = downIndex < floorPlan.Length;
                bool hasLeftBounds = x > 0;
                bool hasRightBounds = x < DungeonGrid.GRID_WIDTH - 1;

                // ---- Send Info to Room Manager ----
                AllDoorsInfo doorInfo = new AllDoorsInfo();
                doorInfo.Up = CreateDoorInfo(hasUpBounds, upIndex, floorPlan);
                doorInfo.Down = CreateDoorInfo(hasDownBounds, downIndex, floorPlan);
                doorInfo.Left = CreateDoorInfo(hasLeftBounds, leftIndex, floorPlan);
                doorInfo.Right = CreateDoorInfo(hasRightBounds, rightIndex, floorPlan);

                rooms.InitDoors(doorInfo);
            }
        }
        public void SetAllActiveFalse()
        {
            foreach (RoomManager rooms in _roomLookup.Values)
            {
                //rooms.gameObject.SetActive(false);
            }
        }
        private DoorInfo CreateDoorInfo(bool hasBounds, int neighbourIndex, int[] floorPlan)
        {
            DoorInfo info = new DoorInfo();

            if (hasBounds && floorPlan[neighbourIndex] != 0)
            {
                RoomManager room = _roomLookup[neighbourIndex];
                info.UnlockOnClear = true;
                info.Material = _doorLookup[room.Type];
            }
            else
            {
                info.UnlockOnClear = false;
                // info.Material = _doorLookup[RoomType.None];
            }

            return info;
        }
    }
}

