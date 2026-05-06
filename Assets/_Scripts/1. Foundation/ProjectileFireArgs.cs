namespace Foundation
{
    /// <summary>
    /// Mutable configuration bag populated by cast rune hooks before
    /// ProjectileAbilityRune fires. Allocated fresh each Activate —
    /// default field values are always the baseline. No reset required.
    /// </summary>
    public sealed class ProjectileFireArgs
    {
        public int   PierceCount    = 0;
        public int   BounceCount    = 0;
        public float SizeMultiplier = 1f;
        public int   HomingCount    = 0;
    }
}