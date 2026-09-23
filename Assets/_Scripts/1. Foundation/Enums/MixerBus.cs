namespace Foundation
{
    /// <summary>
    /// Routing destinations that map 1-to-1 with AudioMixerGroups in the project mixer.
    /// Add new buses here first, then wire the matching group in AudioManager's inspector.
    /// </summary>
    public enum MixerBus
    {
        Master,
        Music,
        SFX,
        UI,
        Ambience
    }
}