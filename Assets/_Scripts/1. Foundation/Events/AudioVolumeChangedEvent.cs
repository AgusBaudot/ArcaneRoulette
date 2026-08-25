namespace Foundation
{
    /// <summary>
    /// Tells the AudioManager to update the mixer immediately
    /// </summary>
    public readonly struct AudioVolumeChangedEvent
    {
        public readonly MixerBus Bus;
        public readonly float Volume;

        public AudioVolumeChangedEvent(MixerBus bus, float volume)
        {
            Bus = bus;
            Volume = volume;
        }
    }
}