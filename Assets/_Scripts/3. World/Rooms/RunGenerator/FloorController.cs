using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    public class FloorController : MonoBehaviour
    {
        [SerializeField] GameObject RegularRoomPrefab;
        [SerializeField] GameObject ItemRoomPrefab;
        [SerializeField] GameObject ShopRoomPrefab;
        [SerializeField] GameObject BossRoomPrefab;
        [SerializeField] GameObject SecretRoomPrefab;
        private int roomSpaceBetween = 15; // Espacio entre rooms
        List<GameObject> spawnedRooms;

        public void Awake()
        {
            spawnedRooms = new List<GameObject>();
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
            int x = index % 10; // Posiciones
            int z = index / 10;

            Vector3 position = new Vector3(x * roomSpaceBetween, 0, -z * roomSpaceBetween);

            GameObject prefab = null;
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
            GameObject newRoom = Instantiate(prefab, position, Quaternion.identity);
            spawnedRooms.Add(newRoom); //Lista de todas las rooms creadas

        }
        public void SetUpDoors(List<GameObject> spawnedRooms) 
        {
            foreach (GameObject rooms in spawnedRooms) 
            {
                
            }
        }
    }
}

