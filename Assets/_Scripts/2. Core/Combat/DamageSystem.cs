using UnityEngine;
using Foundation;

namespace Core
{
    /// <summary>
    /// Single point of damage resolution for prototype.
    /// Post-prototype: replace hardcoded table with ElementalResistanceMap per enemy
    /// Post-prototype: fire VolatileRunState.FireDamageOut / FireDamageIn pipeline here.
    /// </summary>
    public static class DamageSystem
    {
        //These could easily be moved to a global CombatSettings ScriptableObject later
        private const float MULTIPLIER_WEAK = 1.3f;
        private const float MULTIPLIER_RESISTANT = 0.7f;
        private const float MULTIPLIER_IMMUNE = 0.0f;

        public static void Deal(
            IDamageable target,
            GameObject targetObject,
            int baseDamage,
            ElementType attackerElement,
            DamageJuice juice)
        {
            if (target == null)
                return;

            float finalDamage = baseDamage;

            if (target is MonoBehaviour mb && mb.TryGetComponent(out IElementalResistance resistance))
            {
                Effectiveness effectiveness = resistance.GetEffectiveness(attackerElement);
                finalDamage = CalculateElementalDamage(baseDamage, effectiveness);

                //Maybe trigger specific juice based on effectiveness level?
                //e.g. larger flash and heavier hit-stop if attack is WEAK.
            }

            //float multiplier = GetResistance(attackerElement, GetDefenderElement(targetObject));
            //int final = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));

            target.TakeDamage((int)finalDamage, attackerElement);

            CameraShake.AddTrauma(juice.CameraShake);
            HitStop.Apply(juice.HitStop);
            if (targetObject.TryGetComponent<DamageFlash>(out var flash))
                flash.Flash(juice.FlashDuration);
        }
        
        public static void Deal(IDamageable target, GameObject targetObject, int baseDamage, ElementType attackerElement)
            => Deal(target, targetObject, baseDamage, attackerElement, DamageJuice.Default);

        /// <summary>
        /// Overload for callers that already have IDamageable and no GameObject needed
        /// </summary>
        /// <param name="target"></param>
        /// <param name="baseDamage"></param>
        /// <param name="attackerElement"></param>
        public static void Deal(
            IDamageable target,
            int baseDamage,
            ElementType attackerElement)
            => Deal(target, null, baseDamage, attackerElement, DamageJuice.Default);

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