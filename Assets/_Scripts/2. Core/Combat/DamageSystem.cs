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
        //These could easily be moved to a global CombatSettings ScriptableObject later
        private const float MULTIPLIER_WEAK = 1.3f;
        private const float MULTIPLIER_RESISTANT = 0.7f;
        private const float MULTIPLIER_IMMUNE = 0.0f;

        public static DamageResult Deal(
            IDamageable target,
            GameObject targetObject,
            int baseDamage,
            ElementType attackerElement)
        {
            if (target == null)
                return DamageResult.None;

            Effectiveness effectiveness = Effectiveness.Neutral;
            float finalDamage = baseDamage;

            if (target is MonoBehaviour mb && mb.TryGetComponent(out IElementalResistance resistance))
            {
                effectiveness = resistance.GetEffectiveness(attackerElement);
                finalDamage = CalculateElementalDamage(baseDamage, effectiveness);
            }
            
            int clampedDamage = effectiveness == Effectiveness.Immune
                ? 0
                : Mathf.Max(1, (int)finalDamage);

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

        private static float CalculateElementalDamage(float baseDamage, Effectiveness tier)
        {
            return tier switch
            {
                Effectiveness.Weak => baseDamage * MULTIPLIER_WEAK,
                Effectiveness.Resistant => baseDamage * MULTIPLIER_RESISTANT,
                Effectiveness.Immune => baseDamage * MULTIPLIER_IMMUNE,
                _ => baseDamage
            };
        }
    }
}