using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    public class MapGenerator : MonoBehaviour
    {
        private int[] _floorPlan; //Grid
        public int[] getFloorPlan => _floorPlan;

        private int _floorPlanCount; //Cuantas rooms spawnearon
        [Header("Dungeon")]
        //45 room inicial
        // 100 grid
        // Generate Dungeon tiene valores especificos


        [Header("Dungeon Size")]
        [SerializeField] private int _minRooms = 6;
        [SerializeField] private int _maxRooms = 6;
        
        [Header("Special Room Counts")]
        [SerializeField] private int _targetItemRooms = 1;
        [SerializeField] private int _targetShopRooms = 0;
        [SerializeField] private int _targetSecretRooms = 0;
        
        private List<int> _endRooms; //Dead End Room

        //Guardar el Index de salas especiales
        private int bossRoomIndex;
        public int BossRoomIndex => bossRoomIndex; //just a benchmark get for this sprint

        //Converted to lists to support 0, 1, or multiple rooms
        private List<int> _secretRoomIndices = new();
        private List<int> _shopRoomIndices = new();
        private List<int> _itemRoomIndices = new();

        private Queue<int> _cellQueue; // cola de generacion de rooms

        private List<RoomInfo> _spawnedCellsInfo;

        public void Init()
        {
            _spawnedCellsInfo = new List<RoomInfo>();
        }
        public List<RoomInfo> SetupDungeon()
        {
            _spawnedCellsInfo.Clear(); //limpia la lista
            _floorPlan = new int[100]; //Grid 10 x 10
            _floorPlanCount = default;
            _cellQueue = new Queue<int>();
            _endRooms = new List<int>(); // nueva lista

            VisitCell(45); // Centro del mapa
            GenerateDungeon();

            return _spawnedCellsInfo;
        }
        private void GenerateDungeon()
        {
            while (_cellQueue.Count > 0)
            {
                int index = _cellQueue.Dequeue();
                int x = index % 10; // obtenemos la columna

                bool created = false;

                if (x > 1) created |= VisitCell(index - 1); // Busca crear una sala en todas las direcciones posibles a la sala de valor index
                if (x < 9) created |= VisitCell(index + 1);
                if (index > 20) created |= VisitCell(index - 10);
                if (index < 70) created |= VisitCell(index + 10);

                if (created == false) // si no se logr� crear ninguna sala se agrega a la lista de rooms finales
                    _endRooms.Add(index);
            }

            //1 is for boss
            int requiredSpecialRooms = 1 + _targetItemRooms + _targetShopRooms;
            if (_floorPlanCount < _minRooms || _endRooms.Count < requiredSpecialRooms)
            {
                SetupDungeon();
                return;
            }

            SetupSpecialRooms();
        }
        private void SetupSpecialRooms()
        {
            //Clean up old lists
            _secretRoomIndices.Clear();
            _shopRoomIndices.Clear();
            _itemRoomIndices.Clear();

            bool generationSuccessful = true;
            
            bossRoomIndex = _endRooms.Count > 0 ? _endRooms[_endRooms.Count - 1] : -1; // si hay room que sea la ultima en agregarse, si no valor invalido

            if (bossRoomIndex != -1)
            {
                _endRooms.RemoveAt(_endRooms.Count - 1); // Quitamos la bossRoom de la lista de endRooms
            }
            else
            {
                generationSuccessful = false;
            }

            for (int i = 0; i < _targetItemRooms; i++)
            {
                int r = RandomEndRoom();
                if (r == -1)
                {
                    generationSuccessful = false;
                }
                else
                {
                    _itemRoomIndices.Add(r);
                }
            }
            
            for (int i = 0; i < _targetShopRooms; i++)
            {
                int r = RandomEndRoom();
                if (r == -1)
                {
                    generationSuccessful = false;
                }
                else
                {
                    _shopRoomIndices.Add(r);
                }
            }

            for (int i = 0; i < _targetSecretRooms; i++)
            {
                int r = PickSecretRoom();
                if (r == -1)
                {
                    generationSuccessful = false;
                }
                else 
                {
                    _secretRoomIndices.Add(r);
                    SaveRoomInfo(r);
                }
            }

            if (!generationSuccessful)
            {
                SetupDungeon();
                return;
            }

            UpdateSpecialRoomType();
        }
        private void UpdateSpecialRoomType()
        {
            for (int i = 0; i < _spawnedCellsInfo.Count; i++)
            {
                RoomInfo cell = _spawnedCellsInfo[i];

                if (_itemRoomIndices.Contains(cell.index))
                {
                    cell.SetRoomType(RoomType.Item);
                }

                else if (_shopRoomIndices.Contains(cell.index))
                {
                    cell.SetRoomType(RoomType.Shop);
                }

                else if (_secretRoomIndices.Contains(cell.index))
                {
                    cell.SetRoomType(RoomType.Secret);
                }

                else if (cell.index == bossRoomIndex)
                {
                    cell.SetRoomType(RoomType.Boss);
                }

                _spawnedCellsInfo[i] = cell;
            }
        }
        private int RandomEndRoom()
        {
            if (_endRooms.Count == 0) //Mientras existan endRooms
                return -1;

            int randomRoom = Random.Range(0, _endRooms.Count); //endRoomRandom
            int index = _endRooms[randomRoom];

            _endRooms.RemoveAt(randomRoom);

            return index;
        }
        private int PickSecretRoom()
        {
            for (int attempt = 0; attempt < 900; attempt++)
            {
                int x = Mathf.FloorToInt(Random.Range(0f, 1f) * 9) + 1;
                int y = Mathf.FloorToInt(Random.Range(0f, 1f) * 8) + 2;

                int index = y * 10 + x;

                if (_floorPlan[index] != 0)
                {
                    continue;
                }

                if (bossRoomIndex == index - 1 || bossRoomIndex == index + 1 || bossRoomIndex == index + 10 || bossRoomIndex == index - 10)
                {
                    continue;
                }

                if (index - 1 < 0 || index + 1 > _floorPlan.Length || index - 10 < 0 || index + 10 > _floorPlan.Length)
                {
                    continue;
                }

                int neighbours = GetNeighbourCount(index);

                if (neighbours >= 3 || (attempt > 300 && neighbours >= 2) || (attempt > 600 && neighbours >= 1))
                {
                    _floorPlan[index] = 1;
                    return index;
                }
            }

            return -1;
        }
        private int GetNeighbourCount(int index)
        {
            return _floorPlan[index - 10] + _floorPlan[index - 1] + _floorPlan[index + 10] + _floorPlan[index + 1]; // Result between 0 - 4
        }
        private bool VisitCell(int index)
        {
            if (_floorPlan[index] != 0 || GetNeighbourCount(index) > 1 || _floorPlanCount >= _maxRooms || Random.value < 0.5f) // Si se encuentra libre seguimos y no tiene mas de 1 vecino( = 0)
                return false;

            _cellQueue.Enqueue(index); //Guardamos el index de la sala principal
            _floorPlan[index] = 1; // = 1
            _floorPlanCount++;

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
