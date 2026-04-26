using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Amplify")]
    public sealed class AmplifyCastRune : CastRuneSO
    {
        [SerializeField] private float _multiplierPerStack = 0.5f; // 1 stack = 1.5×, 2 = 2×

        //stackCount is captured at subscription time by SpellInstance.
        //The Action writes into the SpellCastModifiers it's given.
        public override void Apply(SpellContext ctx, int stackCount)
        {
            float m = 1f + _multiplierPerStack * stackCount;

            switch (ctx.Ability)
            {
                case IProjectileConfig proj:
                    proj.SizeMultiplier = m;
                    break;
                
                case IDashConfig dash:
                    dash.DurationMultiplier = m;
                    break;
                
                case IShieldConfig shield:
                    shield.RadiusMultiplier = m;
                    break;
            }
        }
    }
}