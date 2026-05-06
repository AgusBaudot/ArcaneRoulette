namespace Foundation
{
    /// <summary>
    /// Base class for all ability runes. Exposes hook events that cast runes
    /// subscribe to at SpellInstance construction time. Hook invocation and
    /// args-object allocation happen inside each ability's own Activate /
    /// StartHold — no central FireCastRunes call, no ResetActiveConfig.
    /// </summary>
    public abstract class AbilityRuneSO : RuneDefinitionSO
    {
        public abstract AbilityType Type { get; }
        public abstract bool IsHoldAbility { get; }
        public abstract float CooldownDuration { get; } //0f = no cooldown.

        // ── Activation lifecycle ─────────────────────────────────────────────
        // Activate: called for instant abilities (Projectile, Dash).
        //   Allocates the appropriate args object, invokes the hook event,
        //   then executes using the populated args.
        // StartHold / HoldTick / StopHold: called for hold abilities (Shield).
        //   StartHold follows the same allocate→invoke→execute pattern.
        public abstract void Activate(SpellContext ctx);
        public abstract void StartHold(SpellContext ctx);
        public abstract void HoldTick(SpellContext ctx, float deltaTime);
        public abstract void StopHold(SpellContext ctx);
    }
}