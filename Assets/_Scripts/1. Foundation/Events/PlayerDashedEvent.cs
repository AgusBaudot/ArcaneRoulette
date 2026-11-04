using UnityEngine;

namespace Foundation
{
    public readonly struct PlayerDashedEvent
    {
        public readonly Vector3 Direction;

        public PlayerDashedEvent(Vector3 direction)
        {
            Direction = direction;
        }
    }
}