using UnityEngine;

namespace Foundation
{
    /// <summary>
    /// Published by anything that wants a currency pickup to appear at a
    /// position, without needing to reference CurrencyDrop (World) directly.
    /// CurrencyDropSpawner (World) is the current listener.
    /// </summary>
    public readonly struct CurrencyDropEvent
    {
        public readonly Vector3 WorldPosition;

        public CurrencyDropEvent(Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
        }
    }
}