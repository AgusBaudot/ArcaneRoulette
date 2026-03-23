namespace Foundation
{
    public abstract class OnHitRuneSO : ModifierRuneSO 
    {
        public abstract void Apply(SpellContext context, int stackCount);
    }
}