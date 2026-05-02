using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Piercing")]
    public sealed class PiercingCastRune : CastRuneSO
    {
        public override void Subscribe(AbilityRuneSO ability, int stackCount, List<Action> cleanup)
        {
            switch (ability)
            {
                case ProjectileAbilityRune proj:
                {
                    Action<ProjectileFireArgs> h = args => args.PierceCount = 3 * stackCount;
                    proj.OnBeforeFire += h;
                    cleanup.Add(() => proj.OnBeforeFire -= h);
                    break;
                }
                case DashAbilityRune dash:
                {
                    Action<DashActivationArgs> h = args => args.DamagesOnDash = true;
                    dash.OnBeforeActivate += h;
                    cleanup.Add(() => dash.OnBeforeActivate -= h);
                    break;
                }
                case ShieldAbilityRune shield:
                {
                    Action<ShieldActivationArgs> h = args => args.AllowEnemyThrough = true;
                    shield.OnBeforeStartHold += h;
                    cleanup.Add(() => shield.OnBeforeStartHold -= h);
                    break;
                }
            }
        }
    }
}