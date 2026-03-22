using UnityEngine;

namespace Core
{
    //PlayerController routes via 'spell is IHoldAbility'
    //Only HoldSpellInstance implements this - SpellInstance never does,
    //so the cast is a reliable discriminator.
    public interface IHoldAbility
    {
        void StartHold();
        void StopHold();
        void HoldTick(float deltaTime);
    }
}