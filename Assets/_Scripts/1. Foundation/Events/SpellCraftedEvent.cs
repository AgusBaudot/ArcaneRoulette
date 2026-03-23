using Core;

namespace Foundation
{
    public readonly struct SpellCraftedEvent
    {
        public readonly SlotIndex Slot;
        public readonly SpellInstance Instance; //SpellInstance is Core - same violation as VolatileRunState slots

        public SpellCraftedEvent(SlotIndex slot, SpellInstance instance)
        {
            Slot = slot;
            Instance = instance;
        }
    }
}