using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace World
{
    public class RoomTypeAssigner
    {
        private MapGeneratorData _data;
        private RoomWeightManager _weightManager;
        private List<RoomInfo> _spawnedCellsInfo = new List<RoomInfo>();

        private int _bossRoomIndex;
        private int _lobbyRoomIndex;
        private List<int> _shopRoomIndices = new();
        private List<int> _restingRoomIndices = new();
        private List<int> _artifactRoomIndices = new();
        private List<int> _regularRoomIndices = new();

        public int GetBossRoomIndex => _bossRoomIndex;
        public int GetLobbyRoomIndex => _lobbyRoomIndex;

        public RoomTypeAssigner(MapGeneratorData data)
        {
            _data = data;
        }
        public (List<RoomInfo> rooms, bool success) AssignRoles(TopologyResult topology)
        {
            ClearData();

            // Save 1 number for each of the guaranteed rooms 
            int guaranteedSpecialCount = CountGuaranteedSpecialRooms();
            int totalAvailableRooms = topology.EndRooms.Count + topology.MiddleRooms.Count + topology.SurroundedRooms.Count;

            if (totalAvailableRooms < (guaranteedSpecialCount + 2))
                return (null, false);

            // ---- boss room ----
            _bossRoomIndex = PullRandomFromList(topology.EndRooms);
            if (_bossRoomIndex == -1) return (null, false);

            // ---- Lobby room ----
            _lobbyRoomIndex = PullRandomFromList(topology.SurroundedRooms);
            if (_lobbyRoomIndex == -1)
            {
                // fallback
                _lobbyRoomIndex = PullRandomFromList(topology.MiddleRooms);
                if (_lobbyRoomIndex == -1) return (null, false);
            }

            List<int> generalPool = new List<int>();
            generalPool.AddRange(topology.EndRooms);
            generalPool.AddRange(topology.MiddleRooms);
            generalPool.AddRange(topology.SurroundedRooms);

            ShuffleList(generalPool);

            int regularRoomsCount = Random.Range(_data.MinCombatRooms, _data.MaxCombatRooms);
            int combatRoomsAssigned = 0;

            // ---- Pick Up rooms ----
            for (int i = 0; i < regularRoomsCount && generalPool.Count > 0; i++)
            {
                _regularRoomIndices.Add(generalPool[generalPool.Count - 1]);
                generalPool.RemoveAt(generalPool.Count - 1);
                combatRoomsAssigned++;
            }

            if (_data.GuaranteeRestingRoom && !AssignPoolToTarget(generalPool, _restingRoomIndices)) return (null, false);
            if (_data.GuaranteeShopRoom && !AssignPoolToTarget(generalPool, _shopRoomIndices)) return (null, false);
            if (_data.GuaranteeArtifactRoom && !AssignPoolToTarget(generalPool, _artifactRoomIndices)) return (null, false);

            while (generalPool.Count > 0)
            {
                int targetIndex = generalPool[generalPool.Count - 1];
                generalPool.RemoveAt(generalPool.Count - 1);

                RoomType dynamicType = _weightManager.GetNextRoom();

                if (dynamicType == RoomType.Combat && combatRoomsAssigned >= _data.MaxCombatRooms)
                {
                    if (_data.GuaranteeRestingRoom) dynamicType = RoomType.Resting;
                    else if (_data.GuaranteeShopRoom) dynamicType = RoomType.Shop;
                    else dynamicType = RoomType.Artifact;
                }

                if (dynamicType == RoomType.Combat) combatRoomsAssigned++;
                AssignToList(targetIndex, dynamicType);
            }

            BuildSpawnedCellsInfo();
            UpdateSpecialRoomType();

            return (_spawnedCellsInfo, true);
        }
        private void ClearData()
        {
            _spawnedCellsInfo.Clear();
            _shopRoomIndices.Clear();
            _restingRoomIndices.Clear();
            _artifactRoomIndices.Clear();
            _regularRoomIndices.Clear();
            _bossRoomIndex = -1;
            _lobbyRoomIndex = -1;
            _weightManager = new RoomWeightManager(startingWeight: 100, penalty: 20, bonus: 40);
        }
        private int CountGuaranteedSpecialRooms()
        {
            int count = 0;
            if (_data.GuaranteeRestingRoom) count++;
            if (_data.GuaranteeShopRoom) count++;
            if (_data.GuaranteeArtifactRoom) count++;
            return count;
        }
        private int PullRandomFromList(List<int> list)
        {
            if (list.Count == 0) return -1;
            int randomIndex = Random.Range(0, list.Count);
            int value = list[randomIndex];
            list.RemoveAt(randomIndex);
            return value;
        }
        private void ShuffleList(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int k = Random.Range(0, i + 1);
                int value = list[k];
                list[k] = list[i];
                list[i] = value;
            }
        }
        private bool AssignPoolToTarget(List<int> pool, List<int> targetList)
        {
            if (pool.Count == 0) return false;
            int index = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            targetList.Add(index);
            return true;
        }
        private void BuildSpawnedCellsInfo()
        {
            HashSet<int> allIndices = new HashSet<int> { _bossRoomIndex, _lobbyRoomIndex };
            allIndices.UnionWith(_restingRoomIndices);
            allIndices.UnionWith(_shopRoomIndices);
            allIndices.UnionWith(_artifactRoomIndices);
            allIndices.UnionWith(_regularRoomIndices);

            foreach (int index in allIndices)
            {
                RoomInfo info = new RoomInfo();
                info.index = index;
                _spawnedCellsInfo.Add(info);
            }
        }
        private void AssignToList(int index, RoomType type)
        {
            switch (type)
            {
                case RoomType.Resting: _restingRoomIndices.Add(index); break;
                case RoomType.Shop: _shopRoomIndices.Add(index); break;
                case RoomType.Artifact: _artifactRoomIndices.Add(index); break;
                default: _regularRoomIndices.Add(index); break;
            }
        }
        private void UpdateSpecialRoomType()
        {
            Dictionary<int, RoomType> typeMap = new Dictionary<int, RoomType>();

            typeMap[_bossRoomIndex] = _data.GenerateBossRoom ? RoomType.Boss : RoomType.Portal;
            typeMap[_lobbyRoomIndex] = RoomType.Lobby;

            foreach (int i in _regularRoomIndices) typeMap[i] = RoomType.Combat;
            foreach (int i in _restingRoomIndices) typeMap[i] = RoomType.Resting;
            foreach (int i in _shopRoomIndices) typeMap[i] = RoomType.Shop;
            foreach (int i in _artifactRoomIndices) typeMap[i] = RoomType.Artifact;

            for (int i = 0; i < _spawnedCellsInfo.Count; i++)
            {
                RoomInfo cell = _spawnedCellsInfo[i];
                if (typeMap.TryGetValue(cell.index, out RoomType type))
                {
                    cell.SetRoomType(type);
                }
                _spawnedCellsInfo[i] = cell;
            }
        }
    }
}