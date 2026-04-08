using System;
using UnityEngine;

namespace Foundation
{
    // Passed to every CastRuneSO.Apply() and OnHitRuneSO.Apply().
    // Lives in Foundation so the abstract SO base classes can reference it without
    // touching Core.
    
    // Runes that need to trigger secondary effects use EventBus.
    public readonly struct SpellContext
    {
        public readonly AbilityType AbilityType;
        public readonly MonoBehaviour Runner; //coroutine host, always PlayerController.

        // Parallel to SpellInstance's deduplicated cast/onhit rune lists (same order).
        // Lets any rune inspect the full modifier composition of the spell —
        // e.g. AoEOnHitRune checking whether PiercingCastRune is present.
        public readonly int[] CastStackCounts;
        public readonly int[] OnHitStackCounts;

        // Populated only during TriggerOnHit; zero/null during cast-phase Apply calls.
        public readonly Vector3    HitPosition;
        public readonly GameObject HitTarget;
        public readonly ElementType AttackerElement;
        public readonly Vector3 AttackerDirection; //zero = repel from HitPosition.
        
        public readonly AbilityRuneSO Ability; //cast runes write config via interface
        public readonly ISpellSource Source; //replaces ActivateWithInstance pattern
        
        //Optional callback - populated by SpellInstance.TriggerOnHit.
        //OnHit runes invoke this to propagate the full OnHit chain to secondary targets.
        //AoEOnHitRune uses this to trigger Knockback, DoT, etx. on area-hit enemies.
        //Null during cast-phase Apply calls.
        public readonly Action<Vector3, GameObject, Vector3> TriggerSecondaryHit;
        
        private SpellContext(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget,
            MonoBehaviour runner,
            ElementType attackerElement,
            Action<Vector3, GameObject, Vector3> triggerSecondaryHit,
            AbilityRuneSO ability,
            ISpellSource source,
            Vector3 attackerDirection)
        {
            AbilityType = abilityType;
            CastStackCounts = castStackCounts;
            OnHitStackCounts = onHitStackCounts;
            HitPosition = hitPosition;
            HitTarget = hitTarget;
            Runner = runner;
            AttackerElement = attackerElement;
            TriggerSecondaryHit = triggerSecondaryHit;
            Ability = ability;
            Source = source;
            AttackerDirection = attackerDirection;
        }

        // Use these factories — never construct directly.
        // Makes call sites self-documenting and keeps HitPosition/HitTarget
        // from being silently default-zeroed on cast contexts.

        public static SpellContext ForCast(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            MonoBehaviour runner,
            ElementType attackerElement,
            AbilityRuneSO ability,
            ISpellSource source)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                Vector3.zero, null, runner, attackerElement, null,
                ability, source, Vector3.zero);

        public static SpellContext ForHit(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget,
            MonoBehaviour runner,
            ElementType  attackerElement = ElementType.Neutral,
            Action<Vector3, GameObject, Vector3> triggerSecondaryHit = null,
            AbilityRuneSO ability = null,
            ISpellSource source = null,
            Vector3 attackerDirection = default)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                                hitPosition, hitTarget, runner, attackerElement, 
                                triggerSecondaryHit, ability, source, attackerDirection);
    }
}