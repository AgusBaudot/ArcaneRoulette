using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


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
        private List<int> _eventRoomIndices = new();
        private List<int> _shopRoomIndices = new();
        private List<int> _restingRoomIndices = new();
        private List<int> _artifactRoomIndices = new();
        private List<int> _regularRoomIndices = new();
        public int BossRoomIndex => _bossRoomIndex;
        public int LobbyRoomIndex => _lobbyRoomIndex;
        #endregion

        #region Const
        private int GridSize => DungeonGrid.GRID_WIDTH * DungeonGrid.GRID_HEIGHT;
        private int StartIndex => (DungeonGrid.GRID_HEIGHT / 2) * DungeonGrid.GRID_WIDTH + (DungeonGrid.GRID_WIDTH / 2);
        private const int SURROUNDED_ROOM_MAX_ATTEMPTS = 900;
        private const int SURROUNDED_ROOM_2_NEIGHBOR = SURROUNDED_ROOM_MAX_ATTEMPTS / 3;
        private const int SURROUNDED_ROOM_1_NEIGHBOR = (SURROUNDED_ROOM_MAX_ATTEMPTS / 3) * 2;
        private readonly RoomType[] _randomPoolTypes = new[]
        {
            //RoomType.Artifact,
            //RoomType.Shop,
            //RoomType.Event,
            RoomType.Resting
        };
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
                _eventRoomIndices.Clear();
                _shopRoomIndices.Clear();
                _restingRoomIndices.Clear();
                _artifactRoomIndices.Clear();
                _regularRoomIndices.Clear();
                _bossRoomIndex = -1;

                VisitCell(StartIndex);
                RunBFS();

                int guaranteedSpecialCount = CountGuaranteedSpecialRooms(); // suma 1 por cada room garantizada

                if (_floorPlanCount < _data.MinRooms || _endRooms.Count < guaranteedSpecialCount) // doble chequear
                    continue;

                if (!SetupSpecialRooms())
                    continue;

                return (_spawnedCellsInfo, true);
            }

            return (null, false);
        }
        private int CountGuaranteedSpecialRooms()
        {
            int count = 0;
            if (_data.GuaranteeRestingRoom) count++;
            if (_data.GuaranteeShopRoom) count++;
            if (_data.GuaranteeEventRoom) count++;
            if (_data.GuaranteeArtifactRoom) count++;
            return count;
        }
        private void RunBFS()
        {
            while (_cellQueue.Count > 0)
            {
                int index = _cellQueue.Dequeue();
                int x = DungeonGrid.GetX(index);
                int y = DungeonGrid.GetY(index);

                bool created = false;

                if (x > 1) created |= VisitCell(index - 1);
                if (x < DungeonGrid.GRID_WIDTH - 2) created |= VisitCell(index + 1);
                if (y > 1) created |= VisitCell(index - DungeonGrid.GRID_WIDTH);
                if (y < DungeonGrid.GRID_HEIGHT - 2) created |= VisitCell(index + DungeonGrid.GRID_WIDTH);

                if (!created)
                    _endRooms.Add(index);
            }
        }
        private bool SetupSpecialRooms()
        {
            int availablePool = _floorPlanCount;

            _bossRoomIndex = _endRooms.Count > 0 ? _endRooms[_endRooms.Count - 1] : -1;
            if (_bossRoomIndex == -1) return false;
            _endRooms.RemoveAt(_endRooms.Count - 1);
            availablePool--;

            _lobbyRoomIndex = PickSurroundedRoom();
            if (_lobbyRoomIndex == -1) return false;
            SaveRoomInfo(_lobbyRoomIndex);
            availablePool--;

            int regularRoomsCount = Random.Range(_data.MinRegularRooms, _data.MaxRegularRooms);
            availablePool -= regularRoomsCount;

            // ---- Pick Up rooms ----
            if (_data.GuaranteeRestingRoom)
            {
                if (!PickRooms(PickEndRoom, _restingRoomIndices)) return false;
                availablePool--;
            }
            if (_data.GuaranteeShopRoom)
            {
                 if (!PickRooms(PickEndRoom, _shopRoomIndices)) return false;
                availablePool--;
            }
            if (_data.GuaranteeEventRoom)
            {
                if (!PickRooms(PickEndRoom, _eventRoomIndices, true)) return false;
                availablePool--;
            }
            if (_data.GuaranteeArtifactRoom)
            {
                if (!PickRooms(PickEndRoom, _artifactRoomIndices)) return false;
                availablePool--;
            }

            HashSet<int> assignedIndices = BuildAssignedSet();
            List<int> unassignedRooms = new List<int>();

            foreach (var info in _spawnedCellsInfo)
            {
                if (!assignedIndices.Contains(info.index))
                    unassignedRooms.Add(info.index);
            }

            for (int i = 0; i < regularRoomsCount && i < unassignedRooms.Count; i++) // priorizo las regular rooms antes que las rooms aleatorias
                _regularRoomIndices.Add(unassignedRooms[i]);


            for (int i = regularRoomsCount; i < unassignedRooms.Count; i++) // indices restantes se asignan aleatoriamente
            {
                RoomType randomType = _randomPoolTypes[Random.Range(0, _randomPoolTypes.Length)]; // tipo random
                AssignToList(unassignedRooms[i], randomType);
            }

            UpdateSpecialRoomType();
            return true;
        }
        private bool PickRooms(Func<int> picker, List<int> targetList, bool saveInfo = false)
        {
            int r = picker();
            if (r == -1) return false;
            targetList.Add(r);
            if (saveInfo) SaveRoomInfo(r);
            return true;
        }
        private HashSet<int> BuildAssignedSet()
        {
            HashSet<int> set = new HashSet<int>();
            set.Add(_bossRoomIndex);
            set.Add(_lobbyRoomIndex);
            foreach (int i in _restingRoomIndices) set.Add(i);
            foreach (int i in _shopRoomIndices) set.Add(i);
            foreach (int i in _eventRoomIndices) set.Add(i);
            foreach (int i in _artifactRoomIndices) set.Add(i);
            return set;
        }
        private void AssignToList(int index, RoomType type)
        {
            switch (type)
            {
                case RoomType.Resting: _restingRoomIndices.Add(index); break;
                case RoomType.Shop: _shopRoomIndices.Add(index); break;
                case RoomType.Event: _eventRoomIndices.Add(index); break;
                case RoomType.Artifact: _artifactRoomIndices.Add(index); break;
            }
        }
        private void UpdateSpecialRoomType()
        {
            Dictionary<int, RoomType> typeMap = new Dictionary<int, RoomType>();

            typeMap[_bossRoomIndex] = RoomType.Boss;
            typeMap[_lobbyRoomIndex] = RoomType.Lobby;

            foreach (int i in _regularRoomIndices) typeMap[i] = RoomType.Regular;
            foreach (int i in _restingRoomIndices) typeMap[i] = RoomType.Resting;
            foreach (int i in _shopRoomIndices) typeMap[i] = RoomType.Shop;
            foreach (int i in _eventRoomIndices) typeMap[i] = RoomType.Event;
            foreach (int i in _artifactRoomIndices) typeMap[i] = RoomType.Artifact;

            for (int i = 0; i < _spawnedCellsInfo.Count; i++)
            {
                RoomInfo cell = _spawnedCellsInfo[i];
                if (typeMap.TryGetValue(cell.index, out RoomType type))
                    cell.SetRoomType(type);
                _spawnedCellsInfo[i] = cell;
            }
        }
        private int PickEndRoom()
        {
            if (_endRooms.Count == 0)
                return -1;

            int randomIndex = Random.Range(0, _endRooms.Count);
            int index = _endRooms[randomIndex];
            _endRooms.RemoveAt(randomIndex);
            return index;
        }
        private int PickSurroundedRoom()
        {
            for (int attempt = 0; attempt < SURROUNDED_ROOM_MAX_ATTEMPTS; attempt++)
            {
                int x = Random.Range(1, DungeonGrid.GRID_WIDTH - 1);
                int y = Random.Range(1, DungeonGrid.GRID_HEIGHT - 1);
                int index = DungeonGrid.GetIndex(x, y);

                if (_floorPlan[index] != 0)
                    continue;

                if (_bossRoomIndex == index - 1 || _bossRoomIndex == index + 1 || _bossRoomIndex == index + DungeonGrid.GRID_WIDTH || _bossRoomIndex == index - DungeonGrid.GRID_WIDTH)
                    continue;

                if (index - 1 < 0 || index + 1 > _floorPlan.Length || index - DungeonGrid.GRID_WIDTH < 0 || index + DungeonGrid.GRID_WIDTH > _floorPlan.Length)
                    continue;

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
            return _floorPlan[index - DungeonGrid.GRID_WIDTH] + _floorPlan[index - 1] + _floorPlan[index + DungeonGrid.GRID_WIDTH] + _floorPlan[index + 1];
        }
        private bool VisitCell(int index)
        {
            if (_floorPlan[index] != 0 || GetNeighbourCount(index) > 1 || _floorPlanCount >= _data.MaxRooms || Random.value < 0.5f)
                return false;

            _cellQueue.Enqueue(index);
            _floorPlan[index] = 1;
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