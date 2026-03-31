using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Bounce")]
    public sealed class BounceCastRune : CastRuneSO
    {
        public override void Apply(SpellContext ctx, int stackCount)
        {
            switch (ctx.AbilityType)
            {
                case AbilityType.Projectile:
                    ctx.Modifiers.BounceCount = 3 * stackCount;
                    break;
                
                case AbilityType.Dash:
                    ctx.Modifiers.BounceCount = 3 * stackCount;
                    break;
                
                case AbilityType.Shield:
                    ctx.Modifiers.ReflectsProjectiles = true;
                    break;
            }
        }
    }
}