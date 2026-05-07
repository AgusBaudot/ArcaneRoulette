namespace Foundation
{
    /// <summary>
    /// Returned by DamageSystem.Deal. Describes what happened to a single hit.
    /// Callers aggregate these across a batch via DamageBatch - never inspect
    /// individual results unless they need per-target data (e.g. effectiveness popups).
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly bool DidDamage;
        public readonly int FinalDamage;
        public readonly Effectiveness Effectiveness;

        public static readonly DamageResult None = default;

        public DamageResult(bool didDamage, int finalDamage, Effectiveness effectiveness)
        {
            DidDamage = didDamage;
            FinalDamage = finalDamage;
            Effectiveness = effectiveness;
        }
    }
}