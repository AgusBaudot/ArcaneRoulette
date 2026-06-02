using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World
{
    public class MapGenerator
    {
        #region Variables
        private MapGeneratorData _data;
        private int[] _floorPlan; //Grid
        private int _floorPlanCount; // total rooms Generated
        private List<int> _endRooms; //Dead End Room
        private Queue<int> _cellQueue; // cola de generacion de rooms
        private List<RoomInfo> _spawnedCellsInfo = new List<RoomInfo>();
        public int[] getFloorPlan => _floorPlan;
        #endregion

        #region Special Indices
        private int _bossRoomIndex;
        private int _lobbyRoomIndex;
        private List<int> _secretRoomIndices = new();
        private List<int> _shopRoomIndices = new();
        private List<int> _restingRoomIndices = new();
        public int BossRoomIndex => _bossRoomIndex;
        public int LobbyRoomIndex => _lobbyRoomIndex;
        #endregion

        #region Const
        private int GridSize => DungeonGrid.GRID_WIDTH * DungeonGrid.GRID_HEIGHT;
        private int StartIndex => (DungeonGrid.GRID_HEIGHT / 2) * DungeonGrid.GRID_WIDTH + (DungeonGrid.GRID_WIDTH / 2);
        private const int SURROUNDED_ROOM_MAX_ATTEMPTS = 900; // 3/3
        private const int SURROUNDED_ROOM_2_NEIGHBOR = SURROUNDED_ROOM_MAX_ATTEMPTS / 3; // 1/3
        private const int SURROUNDED_ROOM_1_NEIGHBOR = (SURROUNDED_ROOM_MAX_ATTEMPTS / 3) * 2; // 2/3
        #endregion
        public void Init(MapGeneratorData mapGeneratorData)
        {
            _data = mapGeneratorData;
        }
        public (List<RoomInfo> rooms, bool success) SetupDungeon()
        {
            int attempts = 0;
            int maxAttempts = 100;

            while (attempts < maxAttempts)
            {
                attempts++;

                _spawnedCellsInfo.Clear();
                _floorPlan = new int[GridSize];
                _floorPlanCount = 0;
                _cellQueue = new Queue<int>();
                _endRooms = new List<int>();
                _secretRoomIndices.Clear();
                _shopRoomIndices.Clear();
                _restingRoomIndices.Clear();
                _bossRoomIndex = -1;

                VisitCell(StartIndex);
                RunBFS();

                int requiredSpecialRooms = _data.targetBossRoom + _data.targetRestingRoom + _data.targetShopRooms;
                if (_floorPlanCount < _data.minRooms || _endRooms.Count < requiredSpecialRooms)
                    continue;

                if (!SetupSpecialRooms())
                    continue;

                return (_spawnedCellsInfo, true);
            }

            return (null, false); // If it was unsuccessful, it returns false.
        }
        private void RunBFS()
        {
            while (_cellQueue.Count > 0)
            {
                int index = _cellQueue.Dequeue();
                int x = DungeonGrid.GetX(index);
                int y = DungeonGrid.GetY(index);

                bool created = false;
                // 1 and 2 are arbitrary values ??for a margin on the edges of the grid 

                if (x > 1) created |= VisitCell(index - 1); // left edge
                if (x < DungeonGrid.GRID_WIDTH - 2) created |= VisitCell(index + 1); // right edge
                if (y > 1) created |= VisitCell(index - DungeonGrid.GRID_WIDTH); // top edge
                if (y < DungeonGrid.GRID_HEIGHT - 2) created |= VisitCell(index + DungeonGrid.GRID_WIDTH); // bottom edge

                if (!created)
                    _endRooms.Add(index);
            }
        }
        private bool SetupSpecialRooms()
        {
            _bossRoomIndex = _endRooms.Count > 0 ? _endRooms[_endRooms.Count - 1] : -1;
            if (_bossRoomIndex == -1) return false;
            _endRooms.RemoveAt(_endRooms.Count - 1);

            _lobbyRoomIndex = PickSurroundedRoom();
            if (_lobbyRoomIndex == -1) return false;
            SaveRoomInfo(_lobbyRoomIndex);

            for (int i = 0; i < _data.targetRestingRoom; i++)
            {
                int r = PickEndRoom();
                if (r == -1) return false;
                _restingRoomIndices.Add(r);
            }

            for (int i = 0; i < _data.targetShopRooms; i++)
            {
                int r = PickEndRoom();
                if (r == -1) return false;
                _shopRoomIndices.Add(r);
            }

            for (int i = 0; i < _data.targetSecretRooms; i++)
            {
                int r = PickSurroundedRoom();
                if (r == -1) return false;
                _secretRoomIndices.Add(r);
                SaveRoomInfo(r);
            }

            UpdateSpecialRoomType();
            return true;
        }
        private void UpdateSpecialRoomType()
        {
            for (int i = 0; i < _spawnedCellsInfo.Count; i++)
            {
                RoomInfo cell = _spawnedCellsInfo[i];

                if (_restingRoomIndices.Contains(cell.index))
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

                else if (cell.index == _bossRoomIndex)
                {
                    cell.SetRoomType(RoomType.Boss);
                }

                else if (cell.index == _lobbyRoomIndex)
                {
                    cell.SetRoomType(RoomType.Lobby);
                }

                _spawnedCellsInfo[i] = cell;
            }
        }
        private int PickEndRoom()
        {
            if (_endRooms.Count == 0) //Mientras existan endRooms
                return -1;

            int randomRoom = Random.Range(0, _endRooms.Count); //endRoomRandom
            int index = _endRooms[randomRoom];

            _endRooms.RemoveAt(randomRoom);

            return index;
        }
        private int PickSurroundedRoom()
        {
            for (int attempt = 0; attempt < SURROUNDED_ROOM_MAX_ATTEMPTS; attempt++)
            {
                int x = Random.Range(1, DungeonGrid.GRID_WIDTH - 1);
                int y = Random.Range(1, DungeonGrid.GRID_HEIGHT - 1);
                int index = DungeonGrid.GetIndex(x,y);

                if (_floorPlan[index] != 0)
                {
                    continue;
                }

                if (_bossRoomIndex == index - 1 || _bossRoomIndex == index + 1 || _bossRoomIndex == index + DungeonGrid.GRID_WIDTH || _bossRoomIndex == index - DungeonGrid.GRID_WIDTH)
                {
                    continue;
                }

                if (index - 1 < 0 || index + 1 > _floorPlan.Length || index - DungeonGrid.GRID_WIDTH < 0 || index + DungeonGrid.GRID_WIDTH > _floorPlan.Length)
                {
                    continue;
                }

                int neighbours = GetNeighbourCount(index);

                if (neighbours >= 3 || (attempt > SURROUNDED_ROOM_2_NEIGHBOR && neighbours >= 2) || (attempt > SURROUNDED_ROOM_1_NEIGHBOR && neighbours >= 1))
                {
                    _floorPlan[index] = 1;
                    return index;
                }
            }

            return -1;
        }
        private int GetNeighbourCount(int index)
        {
            return _floorPlan[index - DungeonGrid.GRID_WIDTH] + _floorPlan[index - 1] + _floorPlan[index + DungeonGrid.GRID_WIDTH] + _floorPlan[index + 1]; // Result between 0 - 4
        }
        private bool VisitCell(int index)
        {
            if (_floorPlan[index] != 0 || GetNeighbourCount(index) > 1 || _floorPlanCount >= _data.maxRooms || Random.value < 0.5f) // Si se encuentra libre seguimos y no tiene mas de 1 vecino( = 0)
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
            newRoomInfo.index = index;
            _spawnedCellsInfo.Add(newRoomInfo);
        }
    }
}