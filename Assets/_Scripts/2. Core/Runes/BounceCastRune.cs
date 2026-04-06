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
            switch (ctx.AbilityType)
            {
                case AbilityType.Projectile:
                    ctx.Modifiers.BounceCount = 3 * stackCount;
                    break;
                
                case AbilityType.Dash:
                case AbilityType.Shield:
                    ctx.Modifiers.ReflectCount = stackCount;
                    ctx.Modifiers.ReflectSpread = _spread;
                    break;
            }
        }
    }
}