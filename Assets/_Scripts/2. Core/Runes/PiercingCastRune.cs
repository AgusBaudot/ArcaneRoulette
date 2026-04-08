using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Piercing")]
    public sealed class PiercingCastRune : CastRuneSO
    {
        public override void Apply(SpellContext ctx, int stackCount)
        {
            switch (ctx.Ability)
            {
                case IProjectileConfig proj:
                    proj.PierceCount = 3 * stackCount;
                    break;
                
                case IDashConfig dash:
                    dash.DamagesOnDash = true;
                    break;
                
                case IShieldConfig shield:
                    shield.AllowEnemyThrough = true;
                    break;
            }
        }
    }
}