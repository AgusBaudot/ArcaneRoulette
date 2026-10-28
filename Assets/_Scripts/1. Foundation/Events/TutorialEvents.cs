using UnityEngine;

namespace Foundation
{
    public readonly struct SpellCraftingOpenedEvent{}
    public readonly struct SpellCraftingClosedEvent{}

    public readonly struct TutorialChestOpenedEvent
    {
        public readonly Vector3 Position;

        public TutorialChestOpenedEvent(Vector3 pos)
        {
            Position = pos;
        }
    }
}