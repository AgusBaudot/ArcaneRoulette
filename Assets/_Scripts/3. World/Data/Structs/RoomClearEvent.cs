using UnityEngine;

namespace Foundation 
{
    public struct RoomClearEvent
    {
        public int Index;
        public Vector3 Position;
        
        public RoomClearEvent(int index, Vector3 position)
        {
            Index = index;
            Position = position;
        }
    }
}
