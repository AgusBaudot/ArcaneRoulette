using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Amplify")]
    public sealed class AmplifyCastRune : CastRuneSO
    {
        [SerializeField] private float _multiplierPerStack = 0.5f; // 1 stack = 1.5×, 2 = 2×

        public override void Apply(SpellContext ctx, int stackCount)
        {
            float multiplier = 1f + _multiplierPerStack * stackCount;

            switch (ctx.AbilityType)
            {
                case AbilityType.Projectile:
                    ctx.Modifiers.SizeMultiplier = multiplier;
                    break;

                case AbilityType.Dash:
                    ctx.Modifiers.DurationMultiplier = multiplier;
                    break;

                case AbilityType.Shield:
                    ctx.Modifiers.RadiusMultiplier = multiplier;
                    break;
            }
        }
    }
}