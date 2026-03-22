namespace Core
{
    //Produced by SpellCrafter when recipe.Ability.IsHoldAbility == true.
    //PlayerController routes hold input via 'spell is IHoldAbility'.
    //Cast runes fire on StartHold (coincides with activation for hold spells).
    //StopHold/HoldTick delegate to the ability rune only - timing of any
    //secondary effects (e.g. AoE on dash end) is owned by the ability rune impl.
    public class HoldSpellInstance : SpellInstance, IHoldAbility
    {
        internal HoldSpellInstance(SpellRecipe recipe) : base(recipe) { }

        public void StartHold()
        {
            var ctx = BuildCastContext();
            Recipe.Ability.StartHold(ctx);
            FireCastRunes(ctx);
        }

        public void StopHold()
        {
            Recipe.Ability.StopHold(BuildCastContext());
        }

        //Calling BuildCastContext() every tick. It's allocation free (struct + array refs already exist)
        //If profiling ever shows it's hot, cache the cast context as a field and invalidate on recipe change.
        public void HoldTick(float deltaTime)
        {
            Recipe.Ability.HoldTick(BuildCastContext(), deltaTime);
        }
    }
}