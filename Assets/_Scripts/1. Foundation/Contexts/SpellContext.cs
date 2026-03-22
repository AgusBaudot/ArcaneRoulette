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

        // Parallel to SpellInstance's deduplicated cast/onhit rune lists (same order).
        // Lets any rune inspect the full modifier composition of the spell —
        // e.g. AoEOnHitRune checking whether PiercingCastRune is present.
        public readonly int[] CastStackCounts;
        public readonly int[] OnHitStackCounts;

        // Populated only during TriggerOnHit; zero/null during cast-phase Apply calls.
        public readonly Vector3    HitPosition;
        public readonly GameObject HitTarget;

        private SpellContext(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget)
        {
            AbilityType      = abilityType;
            CastStackCounts  = castStackCounts;
            OnHitStackCounts = onHitStackCounts;
            HitPosition      = hitPosition;
            HitTarget        = hitTarget;
        }

        // Use these factories — never construct directly.
        // Makes call sites self-documenting and keeps HitPosition/HitTarget
        // from being silently default-zeroed on cast contexts.

        public static SpellContext ForCast(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                                Vector3.zero, null);

        public static SpellContext ForHit(
            AbilityType abilityType,
            int[]       castStackCounts,
            int[]       onHitStackCounts,
            Vector3     hitPosition,
            GameObject  hitTarget)
            => new SpellContext(abilityType, castStackCounts, onHitStackCounts,
                                hitPosition, hitTarget);
    }
}