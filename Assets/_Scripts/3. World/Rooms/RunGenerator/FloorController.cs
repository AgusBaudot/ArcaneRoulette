using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    public class FloorController : MonoBehaviour
    {
        [SerializeField] RoomManager RegularRoomPrefab;
        [SerializeField] RoomManager ItemRoomPrefab;
        [SerializeField] RoomManager ShopRoomPrefab;
        [SerializeField] RoomManager BossRoomPrefab;
        [SerializeField] RoomManager SecretRoomPrefab;
        private int roomSpaceBetween = 15; // Espacio entre rooms
        List<RoomManager> spawnedRooms;

        public void Awake()
        {
            spawnedRooms = new List<RoomManager>();
        }
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("SetUpRooms");
                SetUpRooms(MapGenerator.instance.getSpawnedCellsInfo);
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

            Vector3 position =
                new Vector3(x * roomSpaceBetween, 0, -z * roomSpaceBetween);

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

            RoomManager newRoom =
                Instantiate(prefab, position, Quaternion.identity);

            RoomInfo info = new RoomInfo();

            info.index = index;
            info.value = 1;
            info.roomType = roomType;

            newRoom.Init(info);

            spawnedRooms.Add(newRoom);

        }
        public void SetUpDoors(List<RoomManager> spawnedRooms) 
        {
            foreach (RoomManager rooms in spawnedRooms) 
            {
                
            }
        }
    }
}

