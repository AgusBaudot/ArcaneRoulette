using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Piercing")]
    public sealed class PiercingCastRune : CastRuneSO
    {
        [Header("Scaling Feedback")]
        [Tooltip("Sounds to play on successive pierces. Clamps to the last element if pierces exceed array length.")]
        [SerializeField] private AudioEventSO[] _pierceSounds;
        
        [Tooltip("Hit stop durations for successive pierces. Clamps to the last element.")]
        [SerializeField] private float[] _pierceHitStops;

        public override void Subscribe(AbilityRuneSO ability, ISpellEventSource source, int stackCount, List<Action> cleanup)
        {
            switch (ability)
            {
                case ProjectileAbilityRune:
                {
                    Action<ProjectileFireArgs> h = args => 
                    {
                        args.PierceCount = 3 * stackCount;
                        args.PierceSounds = _pierceSounds;
                        args.PierceHitStops = _pierceHitStops;
                    };
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