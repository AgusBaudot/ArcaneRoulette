using UnityEngine;
using Foundation;

namespace Core
{
    /// <summary>
    /// Single point of damage resolution.
    /// Resolves elemental resistance and applies TakeDamage.
    /// 
    /// Post-prototype: fire VolatileRunState.FireDamageOut / FireDamageIn pipeline here.
    /// </summary>
    public static class DamageSystem
    {
        public static DamageResult Deal(
            IDamageable target,
            GameObject targetObject,
            int baseDamage,
            ElementType attackerElement)
        {
            if (Cheats.GodMode && target is MonoBehaviour mono && mono.GetComponentInParent<PlayerController>() != null)
            {
                return DamageResult.None;
            }
            
            if (target == null)
                return DamageResult.None;

            Effectiveness effectiveness = Effectiveness.Neutral;
            float finalDamage = baseDamage;

            if (target is IElemental elementalTarget)
            {
                effectiveness = Helpers.Combat.GetEffectiveness(elementalTarget.Element, attackerElement);
                float multiplier = Helpers.Combat.GetEffectivenessMultiplier(effectiveness);
                
                finalDamage = baseDamage * multiplier;
            }
            
            int clampedDamage = effectiveness == Effectiveness.Immune
                ? 0
                : Mathf.Max(1, Mathf.RoundToInt(finalDamage));

            if (!target.TakeDamage(clampedDamage, attackerElement))
                return DamageResult.None;
            
            return new DamageResult(true, clampedDamage, effectiveness);
        }
        
        /// <summary>
        /// Overload for callers without an explicit GameObject reference.
        /// Resolves the GO from the IDamageable MoonBehaviour so the resistance
        /// check still runs.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="baseDamage"></param>
        /// <param name="attackerElement"></param>
        /// <returns></returns>
        public static DamageResult Deal(IDamageable target, int baseDamage, ElementType attackerElement)
        => Deal(target, (target as MonoBehaviour)?.gameObject, baseDamage, attackerElement);
    }
}