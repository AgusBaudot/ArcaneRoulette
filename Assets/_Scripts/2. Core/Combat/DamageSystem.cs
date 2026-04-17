using UnityEngine;
using Foundation;
using UnityEditor.Rendering;

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
            => Deal(target, targetObject, baseDamage, attackerElement, new DamageJuice(0.06f, 0.7f, 0.07f));

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
            => Deal(target, null, baseDamage, attackerElement, new DamageJuice(0.06f, 0.7f, 0.07f));

        //PROTOTYPE: flat hardcoded table.
        //Replace with enemy.GetComponent<ElementalResistanceMap>() post-prototype.
        private static float GetResistance(ElementType attacker, ElementType defender)
        {

            return (attacker, defender) switch
            {
                (ElementType.Fire, ElementType.Water) => 0.7f,
                (ElementType.Fire, ElementType.Earth) => 1.3f,
                (ElementType.Earth, ElementType.Fire) => 0.7f,
                (ElementType.Earth, ElementType.Electric) => 1.3f,
                (ElementType.Electric, ElementType.Earth) => 0.7f,
                (ElementType.Electric, ElementType.Water) => 1.3f,
                (ElementType.Water, ElementType.Fire) => 1.3f,
                (ElementType.Water, ElementType.Electric) => 0.7f,
                _ => 1f
            };
        }
        
        //PROTOTYPE: reads ElementType from a component on the target.
        // Enemies expose their element via IElemental - if absent, Neutral.
        private static ElementType GetDefenderElement(GameObject target)
        {
            if (target == null)
                return ElementType.Neutral;

            if (target.TryGetComponent<IElemental>(out var el))
                return el.Element;

            return ElementType.Neutral;
        }

        private static float CalculateElementalDamage(float baseDamage, Effectiveness tier)
        {
            return tier switch
            {
                Effectiveness.Weak => baseDamage * MULTIPLIER_WEAK,
                Effectiveness.Resistant => baseDamage * MULTIPLIER_RESISTANT,
                Effectiveness.Immune => baseDamage * MULTIPLIER_IMMUNE,
                _ => 1f
            };
        }
    }
}