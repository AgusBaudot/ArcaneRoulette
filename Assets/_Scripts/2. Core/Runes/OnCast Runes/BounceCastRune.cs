using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Bounce")]
    public sealed class BounceCastRune : CastRuneSO
    {
        [SerializeField] private float _spread = 60f; //Total arc in degrees across all stacks
 
        public override void Subscribe(AbilityRuneSO ability, int stackCount,
            List<Action> cleanup)
        {
            switch (ability)
            {
                case ProjectileAbilityRune proj:
                {
                    Action<ProjectileFireArgs> h = args => args.BounceCount = 3 * stackCount;
                    proj.OnBeforeFire += h;
                    cleanup.Add(() => proj.OnBeforeFire -= h);
                    break;
                }
                case DashAbilityRune dash:
                {
                    // Capture _spread at subscribe time — same SO, same value,
                    // but closure is explicit for correctness.
                    float spread = _spread;
                    Action<DashActivationArgs> h = args =>
                    {
                        args.ReflectsProjectiles = true;
                        args.ReflectCount        = stackCount;
                        args.ReflectSpread       = spread;
                        args.BounceCount         = stackCount;
                    };
                    dash.OnBeforeActivate += h;
                    cleanup.Add(() => dash.OnBeforeActivate -= h);
                    break;
                }
                case ShieldAbilityRune shield:
                {
                    float spread = _spread;
                    Action<ShieldActivationArgs> h = args =>
                    {
                        args.ReflectsProjectiles = true;
                        args.ReflectCount        = stackCount;
                        args.ReflectSpread       = spread;
                    };
                    shield.OnBeforeStartHold += h;
                    cleanup.Add(() => shield.OnBeforeStartHold -= h);
                    break;
                }
            }
        }
    }
}