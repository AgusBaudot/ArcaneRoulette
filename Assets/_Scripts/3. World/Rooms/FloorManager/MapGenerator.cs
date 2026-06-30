using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


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

                // Generate GridTopology
                var (topology, geoSuccess) = _topologyGenerator.ExecuteLayoutGeneration();
                if (!geoSuccess) continue;

                // Assign RoomType to rooms
                var (finalRooms, assignSuccess) = _typeAssigner.AssignRoles(topology);
                if (!assignSuccess) continue;

                return (finalRooms, true);
            }

            return (null, false);
        }
    }
}