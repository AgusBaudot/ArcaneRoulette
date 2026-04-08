using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Bounce")]
    public sealed class BounceCastRune : CastRuneSO
    {
        [SerializeField] private float _spread = 60f; //Total arc in degrees across all stacks
        
        public override void Apply(SpellContext ctx, int stackCount)
        {
            switch (ctx.Ability)
            {
                case IProjectileConfig proj:
                    proj.BounceCount = 3 * stackCount;
                    break;
                
                case IDashConfig dash:
                    dash.ReflectsProjectiles = true;
                    dash.ReflectCount = stackCount;
                    dash.ReflectSpread = _spread;
                    dash.BounceCount = stackCount;
                    break;
                    
                case IShieldConfig shield:
                    shield.ReflectsProjectiles = true;
                    shield.ReflectCount = stackCount;
                    shield.ReflectSpread = _spread;
                    break;
            }
        }
    }
}