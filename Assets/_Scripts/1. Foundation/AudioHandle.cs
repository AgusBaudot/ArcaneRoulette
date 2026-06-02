namespace Foundation
{
    /// <summary>
    /// Opaque token returned when gameplay code requests a tracked audio playback.
    /// Hold onto this to stop, fade out, or modulate a continuous sound (loops, held SFX).
    ///
    /// Gameplay code must NEVER access Emitter directly - it is internal to the audio system.
    /// Use EventBus.Publish(new AudioStopRequest { Handle = handle }) to stop it,
    /// or EventBus.Publish(new AudioModulateRequest {Handle = handle, ...}) to change pitch/volume.
    ///
    /// Handles become invalid (IsValid = false) once the sound naturally ends or is explicitly stopped.
    /// Always null-check or IsValid-check before re-using a stored handle.
    /// </summary>
    public sealed class AudioHandle
    {
        /// <summary>
        /// True while the associated AudioSource is still active and playing.
        /// </summary>
        public bool IsPlaying { get; internal set; }
        
        /// <summary>
        /// False once the emitter has been returned to the pool - either naturally (clip end)
        /// or via an AudioStopRequest. Storing a handle across frames: always check this.
        /// </summary>
        public bool IsValid { get; internal set; }
        
        /// <summary>
        /// The AudioEventSO that spawned this handle.
        /// Safe to read from gaemplay code for display or logic purposes.
        /// </summary>
        public AudioEventSO Event { get; internal set; }
        
        //Internal reference - Meta band only.
        //Never expose this outside the AudioSystem assembly.
        internal object Emitter; //typed as object to avoid exposing AudioEmitter to Foundation
        
        internal AudioHandle() {}

        /// <summary>
        /// Invalidates the handle. Called by AudioEmitter on despawn.
        /// </summary>
        internal void Invalidate()
        {
            IsPlaying = false;
            IsValid = false;
            Event = null;
        }
    }
}