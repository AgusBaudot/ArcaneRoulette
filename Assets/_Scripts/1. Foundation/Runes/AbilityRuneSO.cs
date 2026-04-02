namespace Foundation
{
    public abstract class AbilityRuneSO : RuneDefinitionSO
    {
        public abstract AbilityType Type { get; }
        public abstract bool IsHoldAbility { get; }
        public abstract float CooldownDuration { get; } //0f = no cooldown.

        // Called by SpellInstance.Activate() for instant abilities,
        // and by HoldSpellInstance.StartHold() for hold abilities.
        public abstract void ResetActiveConfig();

        // Hold-ability lifecycle. Non-hold runes must implement these
        // but they will never be called — SpellCrafter only produces
        // HoldSpellInstance when IsHoldAbility is true.
        public abstract void Activate(SpellContext ctx);
        public abstract void StartHold(SpellContext ctx);
        public abstract void StopHold(SpellContext ctx);
        public abstract void HoldTick(SpellContext ctx, float deltaTime);
    }
}