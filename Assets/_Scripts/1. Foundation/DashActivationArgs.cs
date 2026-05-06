namespace Foundation
{
    /// <summary>
    /// Mutable configuration bag populated by cast rune hooks before
    /// DashAbilityRune activates. Allocated fresh each Activate.
    /// </summary>
    public sealed class DashActivationArgs
    {
        public float DurationMultiplier  = 1f;
        public bool  DamagesOnDash       = false;
        public int   BounceCount         = 0;
        public bool  ReflectsProjectiles = false;
        public int   ReflectCount        = 0;
        public float ReflectSpread       = 0f;
        public int   HomingCount         = 0;
    }
}