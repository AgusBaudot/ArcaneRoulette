using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Piercing")]
    public sealed class PiercingCastRune : CastRuneSO
    {
        public override void Apply(SpellContext ctx, int stackCount)
        {
            switch (ctx.AbilityType)
            {
                case AbilityType.Projectile:
                    ctx.Modifiers.PierceCount = 3 * stackCount;
                    break;
                
                case AbilityType.Dash:
                    ctx.Modifiers.DamagesOnDash = true;
                    break;
                
                case AbilityType.Shield:
                    ctx.Modifiers.AllowEnemyThrough = true;
                    break;
            }
        }
    }
}