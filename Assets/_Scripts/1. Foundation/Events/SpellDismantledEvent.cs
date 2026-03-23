namespace Foundation
{
    public readonly struct SpellDismantledEvent
    {
        public readonly SlotIndex Slot;

        public SpellDismantledEvent(SlotIndex slot)
        {
            Slot = slot;
        }
    }
}