using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Amplify")]
    public sealed class AmplifyCastRune : CastRuneSO
    {
        [SerializeField] private float _multiplierPerStack = 0.5f; // 1 stack = 1.5×, 2 = 2×

        public override void Subscribe(AbilityRuneSO ability, int stackCount,
            List<Action> cleanup)
        {
            // Capture at subscribe time — designer value on SO is stable,
            // but explicit closure prevents subtle bugs if SO is ever hot-reloaded.
            float multiplier = _multiplierPerStack;
 
            if (ability is ProjectileAbilityRune proj)
            {
                Action<ProjectileFireArgs> h =
                    args => args.SizeMultiplier = 1f + multiplier * stackCount;
                proj.OnBeforeFire += h;
                cleanup.Add(() => proj.OnBeforeFire -= h);
            }
 
            if (ability is DashAbilityRune dash)
            {
                Action<DashActivationArgs> h =
                    args => args.DurationMultiplier = 1f + multiplier * stackCount;
                dash.OnBeforeActivate += h;
                cleanup.Add(() => dash.OnBeforeActivate -= h);
            }
 
            if (ability is ShieldAbilityRune shield)
            {
                Action<ShieldActivationArgs> h =
                    args => args.RadiusMultiplier = 1f + multiplier * stackCount;
                shield.OnBeforeStartHold += h;
                cleanup.Add(() => shield.OnBeforeStartHold -= h);
            }
        }
    }
}