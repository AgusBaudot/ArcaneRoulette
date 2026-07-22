namespace Foundation
{
    public interface IAudioEmitter
    {
        void StopImmediate();
        void StopWithFade(float fadeOutDuration);
        void SetPitch(float pitch);
        void SetVolume(float volume);
    }
}