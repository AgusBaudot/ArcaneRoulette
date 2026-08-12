using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/OnHit/DoT")]
    public class DoTOnHitRune : OnHitRuneSO
    {
        [Header("Duration Settings")]
        [SerializeField, Tooltip("Duration at 1 stack (Default: 4s)")] 
        private float _baseDuration = 4f;
        [SerializeField, Tooltip("Duration added per extra stack (Default: 1s)")] 
        private float _additionalDurationPerStack = 1f;
        
        [Header("Damage Settings")]
        [SerializeField, Tooltip("Multiplier applied to Spell's Base Damage (Default: 0.1)")] 
        private float _damageMultiplier = 0.1f;
        [SerializeField, Tooltip("How often the DoT ticks in seconds (Default: 1s)")] 
        private float _tickInterval = 1f;

        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (ctx.HitTarget == null || !ctx.HitTarget.TryGetComponent<IDamageable>(out var dmg))
                return;

            var dotComponent = ctx.HitTarget.GetComponent<DoTComponent>()
                               ?? ctx.HitTarget.AddComponent<DoTComponent>();

            // Linear duration progression: Stack 1 = 4s, Stack 2 = 5s, etc.
            float duration = _baseDuration + (_additionalDurationPerStack * (stackCount - 1));
            
            // Calculate tick damage (Minimum 1 to prevent 0-damage ticks)
            int tickDamage = Mathf.Max(1, Mathf.RoundToInt(Helpers.PlayerStats.BaseDamage * _damageMultiplier));

            // Each hit creates a completely independent instance
            dotComponent.AddDoT(new DoTInstance(tickDamage, duration, _tickInterval, ctx.AttackerElement));
        }
    }
}