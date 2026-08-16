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
        public readonly IStatResolver Stats;

        // Parallel to SpellInstance's deduplicated cast/onhit rune lists (same order).
        // Lets any rune inspect the full modifier composition of the spell —
        // e.g. AoEOnHitRune checking whether PiercingCastRune is present.
        public readonly int[] CastStackCounts;
        public readonly int[] OnHitStackCounts;

        // Populated only during TriggerOnHit; zero/null during cast-phase Apply calls.
        public readonly Vector3 HitPosition;
        public readonly GameObject HitTarget;
        public readonly ElementType AttackerElement;
        public readonly Vector3 AttackerDirection; //zero = repel from HitPosition.
        public readonly bool IsSecondaryHit;
        
        public readonly AbilityRuneSO Ability; //cast runes write config via interface
        public readonly ISpellSource Source; //replaces ActivateWithInstance pattern
        
        private SpellContext(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget,
            MonoBehaviour runner,
            IStatResolver stats,
            ElementType attackerElement,
            AbilityRuneSO ability,
            ISpellSource source,
            Vector3 attackerDirection, 
            bool isSecondaryHit)
        {
            AbilityType = abilityType;
            CastStackCounts = castStackCounts;
            OnHitStackCounts = onHitStackCounts;
            HitPosition = hitPosition;
            HitTarget = hitTarget;
            Runner = runner;
            Stats = stats;
            AttackerElement = attackerElement;
            Ability = ability;
            Source = source;
            AttackerDirection = attackerDirection;
            IsSecondaryHit = isSecondaryHit;
        }

        public void TriggerSecondaryHit(Vector3 hitPos, GameObject target, Vector3 repelDir)
        {
            Source?.TriggerOnHit(hitPos, target, Runner, AbilityType, false, repelDir, true);
        }

        // Use these factories — never construct directly.
        // Makes call sites self-documenting and keeps HitPosition/HitTarget
        // from being silently default-zeroed on cast contexts.

        public static SpellContext ForCast(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            MonoBehaviour runner,
            IStatResolver stats,
            ElementType attackerElement,
            AbilityRuneSO ability,
            ISpellSource source,
            bool isSecondaryHit)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                Vector3.zero, null, runner, stats, attackerElement, 
                ability, source, Vector3.zero, isSecondaryHit);

        public static SpellContext ForHit(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget,
            MonoBehaviour runner,
            IStatResolver stats,
            ElementType  attackerElement = ElementType.Neutral,
            AbilityRuneSO ability = null,
            ISpellSource source = null,
            Vector3 attackerDirection = default,
            bool isSecondaryHit = false)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                                hitPosition, hitTarget, runner, stats, attackerElement, 
                                ability, source, attackerDirection, 
                                isSecondaryHit);
    }
}