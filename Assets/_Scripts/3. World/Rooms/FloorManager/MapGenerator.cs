using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class MapGenerator
    {
        private MapGeneratorData _data;
        private GridTopologyGenerator _topologyGenerator;
        private RoomTypeAssigner _typeAssigner;

        public int[] getFloorPlan => _topologyGenerator?.GetFloorPlan;
        public int BossRoomIndex => _typeAssigner?.GetBossRoomIndex ?? -1;
        public int LobbyRoomIndex => _typeAssigner?.GetLobbyRoomIndex ?? -1;

        public MapGenerator(MapGeneratorData data)
        {
            _data = data;
            _topologyGenerator = new GridTopologyGenerator(_data);
            _typeAssigner = new RoomTypeAssigner(_data);
        }

        public (List<RoomInfo> rooms, bool success) SetupDungeon()
        {
            int attempts = 0;
            int maxAttempts = 100;

            while (attempts < maxAttempts)
            {
                attempts++;

                var (topology, geoSuccess) = _topologyGenerator.ExecuteLayoutGeneration();
                if (!geoSuccess) continue;

                var (finalRooms, assignSuccess) = _typeAssigner.AssignRoles(topology);
                if (!assignSuccess) continue;

                if (!IsEndRoomFarEnough()) continue;

                return (finalRooms, true);
            }
            
            Debug.LogWarning($"<color=yellow>MapGenerator:</color> Failed to generate a valid dungeon after {maxAttempts} attempts. The grid might be too constrained.");
            return (null, false);
        }

        private bool IsEndRoomFarEnough()
        {
            int lobbyIdx = LobbyRoomIndex;
            int targetIdx = BossRoomIndex; 

            if (lobbyIdx == -1 || targetIdx == -1) 
            {
                Debug.LogWarning("MapGenerator: Lobby or Target index is -1. Check RoomTypeAssigner logic.");
                return false;
            }

            int lobbyX = DungeonGrid.GetX(lobbyIdx);
            int lobbyY = DungeonGrid.GetY(lobbyIdx);

            int targetX = DungeonGrid.GetX(targetIdx);
            int targetY = DungeonGrid.GetY(targetIdx);

            int distance = Mathf.Abs(targetX - lobbyX) + Mathf.Abs(targetY - lobbyY);

            return distance >= 3;
        }
    }
}