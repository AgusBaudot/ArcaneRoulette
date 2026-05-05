using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Piercing")]
    public sealed class PiercingCastRune : CastRuneSO
    {
        public override void Subscribe(AbilityRuneSO ability, ISpellEventSource source, int stackCount, List<Action> cleanup)
        {
            switch (ability)
            {
                case ProjectileAbilityRune:
                {
                    Action<ProjectileFireArgs> h = args => args.PierceCount = 3 * stackCount;
                    source.OnBeforeFire += h;
                    cleanup.Add(() => source.OnBeforeFire -= h);
                    break;
                }
                case DashAbilityRune:
                {
                    Action<DashActivationArgs> h = args => args.DamagesOnDash = true;
                    source.OnBeforeActivate += h;
                    cleanup.Add(() => source.OnBeforeActivate -= h);
                    break;
                }
                case ShieldAbilityRune:
                {
                    Action<ShieldActivationArgs> h = args => args.AllowEnemyThrough = true;
                    source.OnBeforeStartHold += h;
                    cleanup.Add(() => source.OnBeforeStartHold -= h);
                    break;
                }
            }
        }
    }
}