using Foundation;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Produced by SpellCrafter for hold abilities (Shield).
    /// Owns an EnergyPool constructed from Helpers.Stats - one independent
    /// pool per instance. Two shield slots therefore have two separate pools
    /// with separate drain/broken/restore state.
    /// </summary>
    public sealed class HoldSpellInstance : SpellInstance, IHoldAbility
    {
        public EnergyPool Energy { get; } = new(Helpers.PlayerStats);
        
        public override ShieldInstanceState ShieldState { get; } = new();
        public override float DisplayProgress => Energy.Current / Energy.Max;
        
        internal HoldSpellInstance(SpellRecipe recipe) : base(recipe) { }

        // ── IHoldAbility ─────────────────────────────────────────────────────
 
        public void StartHold(MonoBehaviour runner)
        {
            // Hook (OnBeforeStartHold) fires inside ShieldAbilityRune.StartHold
            // against freshly allocated ShieldActivationArgs.
            var ctx = BuildCastContext(runner);
            Recipe.Ability.StartHold(ctx);
        }
 
        public void StopHold(MonoBehaviour runner)
        {
            var ctx = BuildCastContext(runner);
            Recipe.Ability.StopHold(ctx);
        }

        public override void Tick(float dt)
        {
            Energy.Tick(dt);
            base.Tick(dt);
        }
 
        public void HoldTick(float deltaTime, MonoBehaviour runner)
        {
            var ctx = BuildCastContext(runner);
            Recipe.Ability.HoldTick(ctx, deltaTime);
        }
    }
}