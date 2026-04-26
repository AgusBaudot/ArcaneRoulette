namespace Foundation
{
    public struct SpellEquippedEvent
    {
        public readonly SlotIndex Slot;
        public readonly ISpellSlot Instance;

        public SpellEquippedEvent(SlotIndex slot, ISpellSlot instance)
        {
            Slot = slot;
            Instance = instance;
        }
    }
}