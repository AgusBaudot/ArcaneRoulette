using System;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    public struct DoorInfo
    {
        public bool unlockOnClear;
        public Material material;
    }
    public struct AllDoorsInfo
    {
        public DoorInfo up;
        public DoorInfo down;
        public DoorInfo left;
        public DoorInfo right;
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

        private int roomSpaceBetween = 15; // Espacio entre rooms
        private List<RoomManager> spawnedRooms;

        public void Awake()
        {
            spawnedRooms = new List<RoomManager>();
            _doorLookup = new Dictionary<RoomType, Material>();

            foreach (DoorScriptable door in doorsMaterials)
            {
                _doorLookup.Add(door.roomType, door.materialDoor);
            }
        }
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("SetUpRooms");
                SetUpRooms(MapGenerator.instance.getSpawnedCellsInfo);
            }
            if (Input.GetKeyDown(KeyCode.B)) 
            {
                SetUpDoors(spawnedRooms, MapGenerator.instance.getFloorPlan);
            }
        }
        public void SetUpRooms(List<RoomInfo> rooms)
        {
            for (int i = 0; i < spawnedRooms.Count; i++) //borra la generacion anterior
            {
                Destroy(spawnedRooms[i].gameObject);
            }
            spawnedRooms.Clear();
            foreach (RoomInfo room in rooms) 
            {
                SpawnRooms(room.index, room.roomType);
            }
            
        }
        public void SpawnRooms(int index, RoomType roomType) 
        {
            int x = index % 10;
            int z = index / 10;

            Vector3 position = new Vector3(x * roomSpaceBetween, 0, -z * roomSpaceBetween);

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

            spawnedRooms.Add(newRoom);
        }
        public void SetUpDoors(List<RoomManager> spawnedRooms, int[] floorPlan) //Un proceso bastante caro deberia hacerse en una pantalla de carga
        {
            foreach (RoomManager rooms in spawnedRooms) 
            {
                // create info for each door
                DoorInfo upInfo = new DoorInfo(); 
                DoorInfo downInfo = new DoorInfo();
                DoorInfo leftInfo = new DoorInfo();
                DoorInfo rightInfo = new DoorInfo();

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

                // ---- Up Door ----
                if (hasUpBounds && floorPlan[upIndex] != 0)
                {
                    RoomManager upRoom = spawnedRooms.Find(x => x.Index == upIndex);
                    Material doorMaterial = _doorLookup[upRoom.Type];
                    upInfo.unlockOnClear = true;
                    upInfo.material = doorMaterial;
                }
                else 
                {
                    Material doorMaterial = _doorLookup[RoomType.none];
                    upInfo.unlockOnClear= false;
                    upInfo.material = doorMaterial;
                }
                // ---- Down Door ----
                if (hasDownBounds && floorPlan[rooms.Index + 10] != 0)
                {
                    RoomManager downRoom = spawnedRooms.Find(x => x.Index == downIndex);
                    Material doorMaterial = _doorLookup[downRoom.Type];
                    downInfo.unlockOnClear = true;
                    downInfo.material = doorMaterial;
                }
                else 
                {
                    Material doorMaterial = _doorLookup[RoomType.none];
                    downInfo.unlockOnClear = false;
                    downInfo.material = doorMaterial;
                }
                // ---- Left Door ----
                if (hasLeftBounds && floorPlan[rooms.Index - 1] != 0)
                {
                    RoomManager leftRoom = spawnedRooms.Find(x => x.Index == leftIndex);
                    Material doorMaterial = _doorLookup[leftRoom.Type];
                    leftInfo.unlockOnClear = true;
                    leftInfo.material = doorMaterial;
                }
                else 
                {
                    Material doorMaterial = _doorLookup[RoomType.none];
                    leftInfo.unlockOnClear = false;
                    leftInfo.material = doorMaterial;
                }
                // ---- Right Door ----
                if (hasRightBounds && floorPlan[rooms.Index + 1] != 0)
                {
                    RoomManager rightRoom = spawnedRooms.Find(x => x.Index == rightIndex);
                    Material doorMaterial = _doorLookup[rightRoom.Type];
                    rightInfo.unlockOnClear = true;
                    rightInfo.material = doorMaterial;
                }
                else 
                {
                    Material doorMaterial = _doorLookup[RoomType.none];
                    rightInfo.unlockOnClear = false;
                    rightInfo.material = doorMaterial;
                }

                // ---- Send Info to Room Manager ----
                AllDoorsInfo doorInfo = new AllDoorsInfo();
                doorInfo.up = upInfo;
                doorInfo.down = downInfo;
                doorInfo.left = leftInfo;
                doorInfo.right = rightInfo;

                rooms.InitDoors(doorInfo);
            }
        }
    }
}

