using System;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    public struct DoorInfo
    {
        public bool UnlockOnClear;
        public Material Material;
    }
    public struct AllDoorsInfo
    {
        public DoorInfo Up;
        public DoorInfo Down;
        public DoorInfo Left;
        public DoorInfo Right;
    }

    public class MapSpawner : MonoBehaviour
    {
        [SerializeField] RoomManager RegularRoomPrefab;
        [SerializeField] RoomManager ItemRoomPrefab;
        [SerializeField] RoomManager ShopRoomPrefab;
        [SerializeField] RoomManager BossRoomPrefab;
        [SerializeField] RoomManager SecretRoomPrefab;

        [SerializeField] DoorScriptable[] doorsMaterials;
        private Dictionary<RoomType, Material> _doorLookup;
        private Dictionary<int, RoomManager> _roomLookup;
        public Dictionary<int, RoomManager> RoomLookup => _roomLookup;

        private int _spaceBetweenRooms = 90;

        public void Awake()
        {
            _doorLookup = new Dictionary<RoomType, Material>();
            _roomLookup = new Dictionary<int, RoomManager>();

            foreach (DoorScriptable door in doorsMaterials)
            {
                _doorLookup.Add(door.roomType, door.materialDoor);
            }
        }
        public void SetUpRooms(List<RoomInfo> rooms, int[] floorPlan)
        {
            foreach (RoomManager room in _roomLookup.Values)
            {
                Destroy(room.gameObject);
            }
            _roomLookup.Clear();

            foreach (RoomInfo room in rooms) 
            {
                SpawnRooms(room.index, room.roomType);
            }

            SetUpDoors(floorPlan);
            SetAllActiveFalse();
        }
        private void SpawnRooms(int index, RoomType roomType) 
        {
            int x = index % 10;
            int z = index / 10;

            Vector3 position = new Vector3(x * _spaceBetweenRooms, 0, -z * _spaceBetweenRooms);

            RoomManager prefab = null;

            switch (roomType)
            {
                case RoomType.Regular:
                    prefab = RegularRoomPrefab;
                    break;

                case RoomType.Boss:
                    prefab = BossRoomPrefab;
                    break;

                case RoomType.Item:
                    prefab = ItemRoomPrefab;
                    break;

                case RoomType.Secret:
                    prefab = SecretRoomPrefab;
                    break;

                case RoomType.Shop:
                    prefab = ShopRoomPrefab;
                    break;
            }

            RoomManager newRoom =Instantiate(prefab, position, Quaternion.identity);

            RoomInfo info = new RoomInfo();

            info.index = index;
            info.value = 1;
            info.roomType = roomType;

            newRoom.Init(info);

            _roomLookup.Add(index,newRoom);
            //spawnedRooms.Add(newRoom);
        }
        public void SetUpDoors(int[] floorPlan)
        {
            foreach (RoomManager rooms in _roomLookup.Values) 
            {
                // save index of neighbours
                int upIndex = rooms.Index - 10;
                int downIndex = rooms.Index + 10;
                int leftIndex = rooms.Index -1;
                int rightIndex = rooms.Index + 1;

                // por si esta fuera del grid
                int x = rooms.Index % 10;
                bool hasUpBounds = upIndex >= 0;
                bool hasDownBounds = downIndex < floorPlan.Length;
                bool hasLeftBounds = x > 0;
                bool hasRightBounds = x < 9;

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
                rooms.gameObject.SetActive(false);
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
                info.Material = _doorLookup[RoomType.None];
            }

            return info;
        }
    }
}

