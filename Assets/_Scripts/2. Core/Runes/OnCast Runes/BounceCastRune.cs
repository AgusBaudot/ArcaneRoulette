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
 
        public override void Subscribe(AbilityRuneSO ability, ISpellEventSource source, int stackCount,
            List<Action> cleanup)
        {
            switch (ability)
            {
                case ProjectileAbilityRune:
                {
                    Action<ProjectileFireArgs> h = args => args.BounceCount = 3 * stackCount;
                    source.OnBeforeFire += h;
                    cleanup.Add(() => source.OnBeforeFire -= h);
                    break;
                }
                case DashAbilityRune:
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
                    source.OnBeforeActivate += h;
                    cleanup.Add(() => source.OnBeforeActivate -= h);
                    break;
                }
                case ShieldAbilityRune:
                {
                    float spread = _spread;
                    Action<ShieldActivationArgs> h = args =>
                    {
                        args.ReflectsProjectiles = true;
                        args.ReflectCount        = stackCount;
                        args.ReflectSpread       = spread;
                    };
                    source.OnBeforeStartHold += h;
                    cleanup.Add(() => source.OnBeforeStartHold -= h);
                    break;
                }
            }
        }
    }
}