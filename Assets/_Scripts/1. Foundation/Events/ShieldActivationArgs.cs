namespace Foundation
{
    /// <summary>
    /// Mutable configuration bag populated by cast rune hooks before
    /// ShieldAbilityRune starts its hold. Allocated fresh each StartHold.
    /// </summary>
    public sealed class ShieldActivationArgs
    {
        public float RadiusMultiplier    = 1f;
        public bool  AllowEnemyThrough   = false;
        public bool  ReflectsProjectiles = false;
        public int   ReflectCount        = 0;
        public float ReflectSpread       = 0f;
        public int   HomingCount         = 0;
    }
}
