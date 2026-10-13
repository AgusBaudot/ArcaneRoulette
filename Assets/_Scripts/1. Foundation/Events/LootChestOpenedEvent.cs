using UnityEngine;

namespace Foundation
{
    public readonly struct LootChestOpenedEvent
    {
        public readonly Vector3 ChestPosition;

        public LootChestOpenedEvent(Vector3 chestPosition)
        {
            ChestPosition = chestPosition;
        }
    }
}