using UnityEngine;

namespace Foundation
{
    public interface ISpellSlot
    {
        AbilityType AbilityType { get; }
        bool IsHoldAbility { get; }
        
        //UI properties
        float DisplayProgress { get; }
        bool IsReady { get; }
        Sprite Icon { get; }
    }
}