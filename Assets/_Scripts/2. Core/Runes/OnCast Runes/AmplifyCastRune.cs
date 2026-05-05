using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Amplify")]
    public sealed class AmplifyCastRune : CastRuneSO
    {
        [SerializeField] private float _multiplierPerStack = 0.5f; // 1 stack = 1.5×, 2 = 2×

        public override void Subscribe(AbilityRuneSO ability, ISpellEventSource source, int stackCount,
            List<Action> cleanup)
        {
            // Capture at subscribe time — designer value on SO is stable,
            // but explicit closure prevents subtle bugs if SO is ever hot-reloaded.
            float multiplier = _multiplierPerStack;
 
            if (ability is ProjectileAbilityRune)
            {
                Action<ProjectileFireArgs> h =
                    args => args.SizeMultiplier = 1f + multiplier * stackCount;
                source.OnBeforeFire += h;
                cleanup.Add(() => source.OnBeforeFire -= h);
            }
 
            if (ability is DashAbilityRune)
            {
                Action<DashActivationArgs> h =
                    args => args.DurationMultiplier = 1f + multiplier * stackCount;
                source.OnBeforeActivate += h;
                cleanup.Add(() => source.OnBeforeActivate -= h);
            }
 
            if (ability is ShieldAbilityRune)
            {
                Action<ShieldActivationArgs> h =
                    args => args.RadiusMultiplier = 1f + multiplier * stackCount;
                source.OnBeforeStartHold += h;
                cleanup.Add(() => source.OnBeforeStartHold -= h);
            }
        }
    }
}