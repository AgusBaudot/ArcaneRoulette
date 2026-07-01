using System.Collections.Generic;

namespace World
{
    public class TopologyResult
    {
        public List<int> EndRooms { get; set; }
        public List<int> SurroundedRooms { get; set; }
        public List<int> MiddleRooms { get; set; }
        public int FloorPlanCount { get; set; }
    }
}
