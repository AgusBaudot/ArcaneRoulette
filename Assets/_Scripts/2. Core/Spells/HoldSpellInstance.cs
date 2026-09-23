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
        
        // ── VOLATILE STATE TRACKING ──────────────────────────────────────────
        // Required by the stateless ShieldAbilityRune to track active objects.
        public GameObject ActiveShieldVisual { get; set; }
        public AudioHandle ActiveHoldAudio { get; set; }
        // ─────────────────────────────────────────────────────────────────────

        public override ShieldInstanceState ShieldState { get; } = new();
        public override float DisplayProgress => Energy.Current / Energy.Max;
        public override bool IsReady => !Energy.IsBroken && Energy.Current > 0f;
        
        // Shadow the base property so the controller recognizes it as a hold ability
        public new bool IsHoldAbility => true; 
        
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
        
        internal override void ApplyProgress(float progress)
        {
            base.ApplyProgress(progress);

            if (Energy != null)
            {
                Energy.InheritProgress(progress);
            }
        }
 
        public void HoldTick(float deltaTime, MonoBehaviour runner)
        {
            var ctx = BuildCastContext(runner);
            Recipe.Ability.HoldTick(ctx, deltaTime);
        }

        internal override void Cleanup()
        {
            if (ActiveShieldVisual != null)
            {
                Object.Destroy(ActiveShieldVisual);
            }

            if (ActiveHoldAudio != null && ActiveHoldAudio.IsValid)
            {
                EventBus.Publish(new AudioStopRequest
                {
                    Handle = ActiveHoldAudio,
                    FadeOut = true
                });
                ActiveHoldAudio = default;
            }
            
            base.Cleanup();
        }
    }
}