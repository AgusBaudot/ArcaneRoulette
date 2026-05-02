using Foundation;
using UnityEngine;

namespace Core
{
    // Produced by SpellCrafter for hold abilities (Shield).
    // Inherits SubscribeRunes and Cleanup from SpellInstance —
    // no additional subscription logic needed here.
    public sealed class HoldSpellInstance : SpellInstance, IHoldAbility
    {
        public override ShieldInstanceState ShieldState { get; } = new();
        public override float DisplayProgress => _energy != null ? _energy.Current / _energy.Max : 1f;

        private PlayerEnergy _energy;

        internal HoldSpellInstance(SpellRecipe recipe) : base(recipe) { }

        // ── IHoldAbility ─────────────────────────────────────────────────────
 
        public void StartHold(MonoBehaviour runner)
        {
            // Hook (OnBeforeStartHold) fires inside ShieldAbilityRune.StartHold
            // against freshly allocated ShieldActivationArgs.
            _energy = ((PlayerController)runner).Energy;
            var ctx = BuildCastContext(runner);
            Recipe.Ability.StartHold(ctx);
        }
 
        public void StopHold(MonoBehaviour runner)
        {
            var ctx = BuildCastContext(runner);
            Recipe.Ability.StopHold(ctx);
        }
 
        public void HoldTick(float deltaTime, MonoBehaviour runner)
        {
            var ctx = BuildCastContext(runner);
            Recipe.Ability.HoldTick(ctx, deltaTime);
        }
    }
}