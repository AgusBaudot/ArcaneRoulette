namespace Foundation
{
    public abstract class AbilityRuneSO : RuneDefinitionSO
    {
        public abstract AbilityType Type { get; }
        public abstract bool IsHoldAbility { get; }

        // Called by SpellInstance.Activate() for instant abilities,
        // and by HoldSpellInstance.StartHold() for hold abilities.
        public abstract void Activate(SpellContext ctx);

        // Hold-ability lifecycle. Non-hold runes must implement these
        // but they will never be called — SpellCrafter only produces
        // HoldSpellInstance when IsHoldAbility is true.
        public abstract void StartHold(SpellContext ctx);
        public abstract void StopHold(SpellContext ctx);
        public abstract void HoldTick(SpellContext ctx, float deltaTime);
    }
}