using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace World
{
    public class GridTopologyGenerator
    {
        private MapGeneratorData _data;
        private int[] _floorPlan; //Grid
        private int _floorPlanCount; // total rooms Generated
        private Queue<int> _cellQueue;

        private List<int> _endRooms = new List<int>();
        private List<int> _surroundedRooms = new();
        private List<int> _middleRooms = new(); // normal rooms 

        private int GridSize => DungeonGrid.GRID_WIDTH * DungeonGrid.GRID_HEIGHT;
        private int StartIndex => (DungeonGrid.GRID_HEIGHT / 2) * DungeonGrid.GRID_WIDTH + (DungeonGrid.GRID_WIDTH / 2);
        private const int SURROUNDED_ROOM_MAX_ATTEMPTS = 900;
        private const int SURROUNDED_ROOM_2_NEIGHBOR = SURROUNDED_ROOM_MAX_ATTEMPTS / 3;
        private const int SURROUNDED_ROOM_1_NEIGHBOR = (SURROUNDED_ROOM_MAX_ATTEMPTS / 3) * 2;
        public int[] GetFloorPlan => _floorPlan;

        public GridTopologyGenerator(MapGeneratorData data)
        {
            _data = data;
        }
        public (TopologyResult result, bool success) ExecuteLayoutGeneration()
        {
            int maxAllowedSurrounded = Mathf.Max(0, _data.MaxRooms - 3);
    
            // Use a local variable for the corrected target, avoiding overwrite the SO data permanently
            int actualSurroundedTarget = Mathf.Min(_data.SurroundedRooms, maxAllowedSurrounded);
            
            ClearData();

            VisitCell(StartIndex);

            RunBFS(actualSurroundedTarget);

            int surroundedTarget = _data.SurroundedRooms;
            GenerateSurroundedRooms(surroundedTarget);

            ClassifyTopology();

            if (_floorPlanCount < _data.MinRooms || _floorPlanCount > _data.MaxRooms)
                return (null, false);

            TopologyResult topology = new TopologyResult
            {
                EndRooms = _endRooms,
                SurroundedRooms = _surroundedRooms,
                MiddleRooms = _middleRooms,
                FloorPlanCount = _floorPlanCount
            };

            return (topology, true);
        }
        private void ClearData()
        {
            _floorPlan = new int[GridSize];
            _floorPlanCount = 0;
            _cellQueue = new Queue<int>();
            _endRooms.Clear();
            _surroundedRooms.Clear();
            _middleRooms.Clear();
        }
        private void RunBFS(int actualSurroundedTarget) 
        {
            while (_cellQueue.Count > 0)
            {
                int index = _cellQueue.Dequeue();
                int x = DungeonGrid.GetX(index);
                int y = DungeonGrid.GetY(index);

                int bfsTargetLimit = _data.MaxRooms - actualSurroundedTarget; 
                bool created = false;

                if (_floorPlanCount < bfsTargetLimit)
                {
                    if (x > 1) created |= VisitCell(index - 1);
                    if (x < DungeonGrid.GRID_WIDTH - 2) created |= VisitCell(index + 1);
                    if (y > 1) created |= VisitCell(index - DungeonGrid.GRID_WIDTH);
                    if (y < DungeonGrid.GRID_HEIGHT - 2) created |= VisitCell(index + DungeonGrid.GRID_WIDTH);
                }

                if (!created)
                    _endRooms.Add(index);
            }
        }
        private void GenerateSurroundedRooms(int count)
        {
            int generated = 0;
            HashSet<int> endRoomsCheck = new HashSet<int>(_endRooms);

            for (int attempt = 0; attempt < SURROUNDED_ROOM_MAX_ATTEMPTS && generated < count; attempt++)
            {
                if (_floorPlanCount >= _data.MaxRooms) break;

                int x = Random.Range(1, DungeonGrid.GRID_WIDTH - 1);
                int y = Random.Range(1, DungeonGrid.GRID_HEIGHT - 1);
                int index = DungeonGrid.GetIndex(x, y);

                if (_floorPlan[index] != 0) continue;

                if (IsNextToAnyEndRoom(index, endRoomsCheck)) continue;

                int neighbours = GetNeighbourCount(index);
                if (neighbours >= 3 || (attempt > SURROUNDED_ROOM_2_NEIGHBOR && neighbours >= 2) || (attempt > SURROUNDED_ROOM_1_NEIGHBOR && neighbours >= 1))
                {
                    _floorPlan[index] = 1;
                    _floorPlanCount++;
                    _surroundedRooms.Add(index);
                    generated++;
                }
            }
        }
        private bool IsNextToAnyEndRoom(int index, HashSet<int> endRooms)
        {
            if (index - DungeonGrid.GRID_WIDTH >= 0 && endRooms.Contains(index - DungeonGrid.GRID_WIDTH)) return true;
            if (index - 1 >= 0 && endRooms.Contains(index - 1)) return true;
            if (index + DungeonGrid.GRID_WIDTH < _floorPlan.Length && endRooms.Contains(index + DungeonGrid.GRID_WIDTH)) return true;
            if (index + 1 < _floorPlan.Length && endRooms.Contains(index + 1)) return true;
            return false;
        }
        private void ClassifyTopology()
        {
            HashSet<int> endRoomsSet = new HashSet<int>(_endRooms);
            HashSet<int> surroundedRoomsSet = new HashSet<int>(_surroundedRooms);

            for (int index = 0; index < _floorPlan.Length; index++)
            {
                if (_floorPlan[index] == 0) continue;

                if (endRoomsSet.Contains(index) || surroundedRoomsSet.Contains(index)) continue;

                _middleRooms.Add(index);
            }
        }
        private int GetNeighbourCount(int index)
        {
            int count = 0;
            if (index - DungeonGrid.GRID_WIDTH >= 0) count += _floorPlan[index - DungeonGrid.GRID_WIDTH];
            if (index - 1 >= 0) count += _floorPlan[index - 1];
            if (index + DungeonGrid.GRID_WIDTH < _floorPlan.Length) count += _floorPlan[index + DungeonGrid.GRID_WIDTH];
            if (index + 1 < _floorPlan.Length) count += _floorPlan[index + 1];
            return count;
        }
        private bool VisitCell(int index)
        {
            if (_floorPlan[index] != 0 || GetNeighbourCount(index) > 1 || _floorPlanCount >= _data.MaxRooms || Random.value < 0.5f)
                return false;

            _cellQueue.Enqueue(index);
            _floorPlan[index] = 1;
            _floorPlanCount++;
            return true;
        }
    }
}
