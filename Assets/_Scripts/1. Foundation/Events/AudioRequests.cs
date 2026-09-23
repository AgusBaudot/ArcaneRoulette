using System;
using UnityEngine;

namespace Foundation
{
    // ═══════════════════════════════════════════════════════════════════════════
    // AUDIO EVENTBUS PAYLOADS
    //All audio interaction from gameplay code goes through one of these.
    //Publish via: EventBus.Publish(new AudioXxxRequest {...});
    //AudioManager subscribes to all of them in its OnEnable.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fire-and-forget playback. For one-shots or looping sounds we don't need to stop manually.
    /// For looping or continuous sounds that need to be stopped, use AudioPlayTrackedRequest instead.
    /// </summary>
    public struct AudioPlayRequest
    {
        /// <summary>
        /// The AudioEventSO describing the sound to play.
        /// </summary>
        public AudioEventSO Event;

        /// <summary>
        /// World-space position for 3D spatialized sounds.
        /// Ignored when Event.Is3D is false
        /// </summary>
        public Vector3 WorldPosition;
    }

    /// <summary>
    /// Plays a sound and returns an AudiHandle via callback.
    /// Use for loops, engine hus, laser beambs, or anything we need to stop/modulate later.
    /// The callback fires synchronously on the same frame.
    /// </summary>
    public struct AudioPlayTrackedRequest
    {
        public AudioEventSO Event;
        public Vector3 WorldPosition;

        /// <summary>
        /// Invoked immediately with the handle. Store it: _myHandle = handle.
        /// The handle becomes invalid (IsValid = false) when the sound ends or is stopped.
        /// </summary>
        public Action<AudioHandle> OnHandleReady;
    }
    
    /// <summary>
    /// Stop a tracked sound, optionally fading it out using Event.FadeOurDuration.
    /// No-ops gracefully if the handle is already invalid.
    /// </summary>
    public struct AudioStopRequest
    {
        public AudioHandle Handle;

        /// <summary>
        /// True = fade out over Event.FadeOutDuration seconds before releasing to pool.
        /// False = stop immediately.
        /// </summary>
        public bool FadeOut;
    }

    /// <summary>
    /// Smoothly crossfade from the currently playing music track to a new one.
    /// AudioManager manages two Music emitter slots (A/B) and blends between them.
    /// </summary>
    public struct AudioCrossfadeRequest
    {
        /// <summary>
        /// The new music track to fade in. Null = fade out current track with no replacement.
        /// </summary>
        public AudioEventSO NewTrack;

        /// <summary>
        /// Duration of the crossfade in seconds. 0 = hard cut.
        /// </summary>
        public float Duration;
    }

    /// <summary>
    /// Temporarily duck (lower) a mixer bus during an important sound, then restore it.
    /// Useful for voide lines, critical warnings, or impact moments.
    /// Concurrent ducks on the same bus take the minimum (deepest) value.
    /// Uses unscaled time - works correctly at timeScale=0.
    /// </summary>
    public struct AudioDuckRequest
    {
        /// <summary>
        /// The bus to duck. Typically, MixerBus.Music or MixerBus.Ambience.
        /// </summary>
        public MixerBus TargetBus;
        
        /// <summary>
        /// Normalised target volume (0-1) to duck to. 0.3 = duck to 30% of full.
        /// </summary>
        [Range(0f, 1f)]
        public float DuckToVolume;

        /// <summary>
        /// Seconds to ramp down to DuckToVolume.
        /// </summary>
        public float AttackSeconds;

        /// <summary>
        /// Seconds to hold at DuckToVolume before releasing.
        /// </summary>
        public float HoldSeconds;
        
        /// <summary>
        /// Seconds to tamp back up to full volume after hold.
        /// </summary>
        public float ReleaseSeconds;
    }

    /// <summary>
    /// Modulate pitch or volume on a live tracked sound in real-time.
    /// Typical use: engine hum pitch tracks player velocity, held beam volume tracks charge level.
    /// No-ops gracefully if the handle is invalid.
    /// </summary>
    public struct AudioModulateRequest
    {
        public AudioHandle Handle;

        /// <summary>
        /// New pitch value. Null = do not change pitch.
        /// </summary>
        public float? Pitch;

        /// <summary>
        /// New volume (0-1). Null = do not change volume.
        /// </summary>
        public float? Volume;
    }
}