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

        public SpellRecipe Recipe => _recipe;
        public AbilityType AbilityType => _recipe.Ability.Type;

        internal SpellInstance(SpellRecipe recipe)
        {
            _recipe = recipe;
            BuildDedupedLists(recipe,
                out _castRunes, out _castCounts,
                out _onHitRunes, out _onHitCounts);
        }

        //IAbility

        public void Activate()
        {
            var ctx = BuildCastContext();
            _recipe.Ability.Activate(ctx);
            FireCastRunes(ctx);
        }

        //OnHit trigger (called by projectile/contact on impact)

        public void TriggerOnHit(Vector3 position, GameObject target)
        {
            var ctx = SpellContext.ForHit(
                _recipe.Ability.Type, _castCounts, _onHitCounts, position, target);
            FireOnHitRunes(ctx);
        }

        //Shared helpers (used by HoldSpellInstance)

        //Exposes a cast-phase context without leaking the raw count arrays.
        protected SpellContext BuildCastContext()
            => SpellContext.ForCast(_recipe.Ability.Type, _castCounts, _onHitCounts);

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