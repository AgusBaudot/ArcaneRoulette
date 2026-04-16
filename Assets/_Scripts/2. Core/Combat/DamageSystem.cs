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
            ElementType attackerElement,
            DamageJuice juice)
        {
            if (target == null)
                return;

            float multiplier = GetResistance(attackerElement, GetDefenderElement(targetObject));
            int final = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));

            target.TakeDamage(final, attackerElement);

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
            => (attacker, defender) switch
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