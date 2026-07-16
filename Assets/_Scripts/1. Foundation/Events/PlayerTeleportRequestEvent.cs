using UnityEngine;

namespace Foundation
{
    public struct PlayerTeleportRequestEvent
    {
        public readonly Vector3 Position;
        
        public PlayerTeleportRequestEvent(Vector3 position)
        {
            Position = position;
        }
    }
}