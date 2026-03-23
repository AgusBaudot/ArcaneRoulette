using Foundation;
using UnityEngine;

namespace Core
{
    //Produced by SpellCrafter when recipe.Ability.IsHoldAbility == true.
    //PlayerController routes hold input via 'spell is IHoldAbility'.
    //Cast runes fire on StartHold (coincides with activation for hold spells).
    //StopHold/HoldTick delegate to the ability rune only - timing of any
    //secondary effects (e.g. AoE on dash end) is owned by the ability rune impl.
    public sealed class HoldSpellInstance : SpellInstance, IHoldAbility, ISpellSlot
    {
        public new bool IsHoldAbility => false;

        internal HoldSpellInstance(SpellRecipe recipe) : base(recipe) { }

        public void StartHold(MonoBehaviour runner)
        {
            var ctx = BuildCastContext(runner);
            FireCastRunes(ctx); //Cast runes apply on hold start.
            Recipe.Ability.StartHold(ctx);
        }

        public void StopHold(MonoBehaviour runner)
            => Recipe.Ability.StopHold(BuildCastContext(runner));

        //Calling BuildCastContext() every tick. It's allocation free (struct + array refs already exist)
        //If profiling ever shows it's hot, cache the cast context as a field and invalidate on recipe change.
        public void HoldTick(float deltaTime, MonoBehaviour runner)
            => Recipe.Ability.HoldTick(BuildCastContext(runner), deltaTime);
    }
}