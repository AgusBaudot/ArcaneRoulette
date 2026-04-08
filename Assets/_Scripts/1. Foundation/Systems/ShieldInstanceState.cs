namespace Foundation
{
    /// <summary>
    /// Owned by HoldSpellInstance. ShieldAbilityRune reads/writes this
    /// via ISpellSource - one bag per equipped spell, not one per SO asset.
    /// </summary>
    public sealed class ShieldInstanceState
    {
        public bool Active = false;
        public float TimeHeld = 0f;
    }
}