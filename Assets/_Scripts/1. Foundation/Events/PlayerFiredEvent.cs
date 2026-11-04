using UnityEngine;

namespace Foundation
{
    public readonly struct PlayerFiredEvent
    {
        public readonly Vector3 AimDirection;

        public PlayerFiredEvent(Vector3 aimDirection)
        {
            AimDirection = aimDirection;
        }
    }
}