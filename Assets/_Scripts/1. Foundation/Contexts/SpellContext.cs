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
        public readonly SpellCastModifiers Modifiers; //writable by cast runes; read by ability
        public readonly ElementType AttackerElement;
        
        private SpellContext(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget,
            MonoBehaviour runner,
            SpellCastModifiers modifiers,
            ElementType attackerElement)
        {
            AbilityType      = abilityType;
            CastStackCounts  = castStackCounts;
            OnHitStackCounts = onHitStackCounts;
            HitPosition      = hitPosition;
            HitTarget        = hitTarget;
            Runner = runner;
            Modifiers = modifiers;
            AttackerElement = attackerElement;
        }

        // Use these factories — never construct directly.
        // Makes call sites self-documenting and keeps HitPosition/HitTarget
        // from being silently default-zeroed on cast contexts.

        public static SpellContext ForCast(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            MonoBehaviour runner,
            ElementType attackerElement = ElementType.Neutral)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                                Vector3.zero, null, runner, new SpellCastModifiers(), attackerElement);

        public static SpellContext ForHit(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget,
            MonoBehaviour runner,
            ElementType  attackerElement = ElementType.Neutral)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                                hitPosition, hitTarget, runner, new SpellCastModifiers(), attackerElement);
    }
}