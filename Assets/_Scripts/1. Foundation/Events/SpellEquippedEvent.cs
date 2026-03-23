using Core;

namespace Foundation
{
    public struct SpellEquippedEvent
    {
        public readonly SlotIndex Slot;
        public readonly SpellInstance Instance;

        public SpellEquippedEvent(SlotIndex slot, SpellInstance instance)
        {
            Slot = slot;
            Instance = instance;
        }
    }
}