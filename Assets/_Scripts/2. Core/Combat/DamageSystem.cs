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
        public static void Deal(
            IDamageable target,
            GameObject targetObject,
            int baseDamage,
            ElementType attackerElement)
        {
            if (target == null)
                return;

            float multiplier = GetResistance(attackerElement, GetDefenderElement(targetObject));
            int final = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));

            target.TakeDamage(final, attackerElement);
        }
        
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
            => Deal(target, null, baseDamage, attackerElement);

        //PROTOTYPE: flas hardcoded table.
        //Replace with enemy.GetComponent<ElementalResistanceMap>() post-prototype.
        private static float GetResistance(ElementType attacker, ElementType defender)
            => (attacker, defender) switch
            {
                (ElementType.Fire, ElementType.Water) => 0.5f,
                (ElementType.Water, ElementType.Fire) => 1.5f,
                (ElementType.Electric, ElementType.Water) => 1.5f,
                _ => 1f
            };
        
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
    }
}