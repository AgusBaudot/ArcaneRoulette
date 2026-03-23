namespace Foundation
{
    public interface ISpellSlot
    {
        AbilityType AbilityType { get; }
        bool IsHoldAbility { get; }
    }
}