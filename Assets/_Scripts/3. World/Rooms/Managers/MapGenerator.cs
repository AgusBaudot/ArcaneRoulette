using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    public class MapGenerator : MonoBehaviour
    {
        private int[] floorPlan; //Grid
        public int[] getFlorrPlan => floorPlan;
        public int[] getFloorPlan => floorPlan;

        private int floorPlanCount; //Cuantas rooms spawnearon
        private int minRooms;
        private int maxRooms;
        private List<int> endRooms; //Dead End Room

        //Guardar el Index de salas especiales
        private int bossRoomIndex;
        private int secretRoomIndex;
        private int shopRoomIndex;
        private int itemRoomIndex;

        private Queue<int> cellQueue; // cola de generacion de rooms

        private List<RoomInfo> _spawnedCellsInfo;

        public void Awake()
        {
            minRooms = 7;
            maxRooms = 15;
            _spawnedCellsInfo = new List<RoomInfo>();
        }
        public List<RoomInfo> SetupDungeon()
        {
            _spawnedCellsInfo.Clear(); //limpia la lista
            floorPlan = new int[100]; //Grid 10 x 10
            floorPlanCount = default;
            cellQueue = new Queue<int>();
            endRooms = new List<int>(); // nueva lista

            VisitCell(45); // Centro del mapa
            GenerateDungeon();

            return _spawnedCellsInfo;
        }
        private void GenerateDungeon()
        {
            while (cellQueue.Count > 0)
            {
                int index = cellQueue.Dequeue();
                int x = index % 10; // obtenemos la columna

                bool created = false;

                if (x > 1) created |= VisitCell(index - 1); // Busca crear una sala en todas las direcciones posibles a la sala de valor index
                if (x < 9) created |= VisitCell(index + 1);
                if (index > 20) created |= VisitCell(index - 10);
                if (index < 70) created |= VisitCell(index + 10);

                if (created == false) // si no se logró crear ninguna sala se agrega a la lista de rooms finales
                    endRooms.Add(index);
            }

            if (floorPlanCount < minRooms) // si no se logro crear el minimo de salas entonces vuelve a empezar
            {
                SetupDungeon();
                return;
            }

            SetupSpecialRooms();
        }
        private void SetupSpecialRooms()
        {
            bossRoomIndex = endRooms.Count > 0 ? endRooms[endRooms.Count - 1] : -1; // si hay room que sea la ultima en agregarse, si no valor invalido

            if (bossRoomIndex != -1)
            {
                endRooms.RemoveAt(endRooms.Count - 1); // Quitamos la bossRoom de la lista de endRooms
            }

            itemRoomIndex = RandomEndRoom(); //
            shopRoomIndex = RandomEndRoom();
            secretRoomIndex = PickSecretRoom();

            if (itemRoomIndex == -1 || shopRoomIndex == -1 || bossRoomIndex == -1 || secretRoomIndex == -1) // si todo sale mal volver a empezar
            {
                SetupDungeon();
                return;
            }

            SaveRoomInfo(secretRoomIndex);

            UpdateSpecialRoomVisual();
        }
        private void UpdateSpecialRoomVisual()
        {
            for (int i = 0; i < _spawnedCellsInfo.Count; i++)
            {
                RoomInfo cell = _spawnedCellsInfo[i];

                if (cell.index == itemRoomIndex)
                {
                    cell.SetRoomType(RoomType.Item);
                }

                if (cell.index == shopRoomIndex)
                {
                    cell.SetRoomType(RoomType.Shop);
                }

                if (cell.index == bossRoomIndex)
                {
                    cell.SetRoomType(RoomType.Boss);
                }

                if (cell.index == secretRoomIndex)
                {
                    cell.SetRoomType(RoomType.Secret);
                }

                _spawnedCellsInfo[i] = cell;
            }
        }
        private int RandomEndRoom()
        {
            if (endRooms.Count == 0) //Mientras existan endRooms
                return -1;

            int randomRoom = Random.Range(0, endRooms.Count); //endRoomRandom
            int index = endRooms[randomRoom];

            endRooms.RemoveAt(randomRoom);

            return index;
        }
        private int PickSecretRoom()
        {
            for (int attempt = 0; attempt < 900; attempt++)
            {
                int x = Mathf.FloorToInt(Random.Range(0f, 1f) * 9) + 1;
                int y = Mathf.FloorToInt(Random.Range(0f, 1f) * 8) + 2;

                int index = y * 10 + x;

                if (floorPlan[index] != 0)
                {
                    continue;
                }

                if (bossRoomIndex == index - 1 || bossRoomIndex == index + 1 || bossRoomIndex == index + 10 || bossRoomIndex == index - 10)
                {
                    continue;
                }

                if (index - 1 < 0 || index + 1 > floorPlan.Length || index - 10 < 0 || index + 10 > floorPlan.Length)
                {
                    continue;
                }

                int neighbours = GetNeighbourCount(index);

                if (neighbours >= 3 || (attempt > 300 && neighbours >= 2) || (attempt > 600 && neighbours >= 1))
                {
                    floorPlan[index] = 1;
                    return index;
                }
            }

            return -1;
        }
        private int GetNeighbourCount(int index)
        {
            return floorPlan[index - 10] + floorPlan[index - 1] + floorPlan[index + 10] + floorPlan[index + 1]; // Result between 0 - 4
        }
        private bool VisitCell(int index)
        {
            if (floorPlan[index] != 0 || GetNeighbourCount(index) > 1 || floorPlanCount > maxRooms || Random.value < 0.5f) // Si se encuentra libre seguimos y no tiene mas de 1 vecino( = 0)
                return false;

            cellQueue.Enqueue(index); //Guardamos el index de la sala principal
            floorPlan[index] = 1; // = 1
            floorPlanCount++;

            SaveRoomInfo(index);

            return true;
        }
        private void SaveRoomInfo(int index)
        {
            RoomInfo newRoomInfo = new RoomInfo();
            newRoomInfo.value = 1;
            newRoomInfo.index = index;
            _spawnedCellsInfo.Add(newRoomInfo);
        }
    }

}
