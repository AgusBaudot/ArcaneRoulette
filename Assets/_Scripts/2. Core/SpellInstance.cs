using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    /// <summary>
    /// Runtime snapshot of an equipped spell. Pure data, no behaviour.
    /// Created by AttunementSystem from a SpellDefinition SO.
    /// Foundation holds this so VolatileRunState can reference it legally.
    /// </summary>

    //SpellCrafter is the only factory. Never construct directly.
    //For hold abilities SpellCrafter produces HoldSpellInstance instead.
    public class SpellInstance : IAbility, ISpellSlot
    {
        private readonly SpellRecipe _recipe;
        private readonly List<CastRuneSO> _castRunes;
        private readonly List<OnHitRuneSO> _onHitRunes;
        private readonly int[] _castCounts;
        private readonly int[] _onHitCounts;

        private float _cooldownRemaining;

        public SpellRecipe Recipe => _recipe;
        public AbilityType AbilityType => _recipe.Ability.Type;
        public bool IsHoldAbility => false;
        public bool IsReady => _cooldownRemaining <= 0f;
        
        //Expose element for Projectile's TakeDamage call until DamageSystem is wired.
        public ElementType? Element => _recipe.HasElement
            ? _recipe.Element.Element
            : ElementType.Neutral;

        internal SpellInstance(SpellRecipe recipe)
        {
            _recipe = recipe;
            BuildDedupedLists(recipe,
                out _castRunes, out _castCounts,
                out _onHitRunes, out _onHitCounts);
        }
        
        //Tick: called by PlayerController every Update

        public void Tick(float dt)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= dt;
        }

        //IAbility

        public void Activate(MonoBehaviour runner)
        {
            if (!IsReady) return;
            
            //Fresh modifiers allocated inside ForCast - cast runes write, ability reads.
            var ctx = BuildCastContext(runner);
            
            //Cast runes MUST fire before Activate so Modifiers are populated.
            FireCastRunes(ctx);

            if (_recipe.Ability is ProjectileAbilityRune proj)
                proj.ActivateWithInstance(ctx, this); //needs SpellInstance for Projectile.Init
            else if (_recipe.Ability is DashAbilityRune dash)
                dash.ActivateWithInstance(ctx, this);
            else
                _recipe.Ability.Activate(ctx);

            _cooldownRemaining = _recipe.Ability.CooldownDuration;
        }

        //OnHit trigger (called by projectile/contact on impact)

        public void TriggerOnHit(Vector3 position, GameObject target, MonoBehaviour runner)
        {
            var ctx = SpellContext.ForHit(
                _recipe.Ability.Type, _castCounts, _onHitCounts, position, target, runner);
            FireOnHitRunes(ctx);
        }

        //Shared helpers (used by HoldSpellInstance)

        //Exposes a cast-phase context without leaking the raw count arrays.
        protected SpellContext BuildCastContext(MonoBehaviour runner)
            => SpellContext.ForCast(_recipe.Ability.Type, _castCounts, _onHitCounts, runner);

        protected void FireCastRunes(SpellContext ctx)
        {
            for (int i = 0; i < _castRunes.Count; i++)
                _castRunes[i].Apply(ctx, _castCounts[i]);
        }

        protected void FireOnHitRunes(SpellContext ctx)
        {
            for (int i = 0; i < _onHitRunes.Count; i++)
                _onHitRunes[i].Apply(ctx, _onHitCounts[i]);
        }

        //Rune list construction

        // Deduplicates modifier slots by SO reference (same asset = same rune).
        // Slot order is irrelevant per spec; only identity and count matter.
        private static void BuildDedupedLists(
            SpellRecipe recipe,
            out List<CastRuneSO> castRunes, out int[] castCounts,
            out List<OnHitRuneSO> onHitRunes, out int[] onHitCounts)
        {
            var castDict = new Dictionary<CastRuneSO, int>();
            var onHitDict = new Dictionary<OnHitRuneSO, int>();

            foreach (var modifier in recipe.Modifiers)
            {
                if (modifier == null) continue;

                if (modifier is CastRuneSO cast)
                    castDict[cast] = castDict.TryGetValue(cast, out var cc) ? cc + 1 : 1;
                else if (modifier is OnHitRuneSO onHit)
                    onHitDict[onHit] = onHitDict.TryGetValue(onHit, out var oc) ? oc + 1 : 1;
            }

            castRunes = new List<CastRuneSO>(castDict.Keys);
            castCounts = new int[castRunes.Count];
            for (int i = 0; i < castRunes.Count; i++)
                castCounts[i] = castDict[castRunes[i]];

            onHitRunes = new List<OnHitRuneSO>(onHitDict.Keys);
            onHitCounts = new int[onHitRunes.Count];
            for (int i = 0; i < onHitRunes.Count; i++)
                onHitCounts[i] = onHitDict[onHitRunes[i]];
        }
    }
}