using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Audio;

namespace Foundation
{
    /// <summary>
    /// Data container that fully described how a sound should be played.
    /// Gameplay code never passes raw AudioClips - it always fires an AudioEventSO.
    ///
    /// Usage:
    ///     One-shot: EventBus.Publish(new AudioPlayRequest {Event = myEvent});
    ///     Positional: EventBus.Publish(new AudioPlayRequest {Event = myEvent, WorldPosition = transform.position});
    ///     Tracked: EventBus.Publish(new AudioPlayTrackedRequest {Event = myEvent, OnHandleReady = h => _handle = h});
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/Audio/Audio Event", fileName = "AudioEvent_")]
    public class AudioEventSO : ScriptableObject
    {
        #region SerializedFields

        // ── Clips ──────────────────────────────────────────────────────────────
        [Header("Clips")]
        [Tooltip("One clip = always play that clip. Multiple = random selection, no consecutive repeats.")]
        [SerializeField] private AudioClip[] _clips;
        
        // ── Volume & Pitch ──────────────────────────────────────────────────────────────
        [Header("Volume & Pitch")]
        [Tooltip("Volume is chosen randomly between min (x) and max (y) each play.")]
        [SerializeField] private Vector2 _volumeRange = new(0.9f, 1.0f);
        
        [Tooltip("Pitch is chosen randomly between min (x) and max (y) each play.")]
        [SerializeField] private Vector2 _pitchRange = new(0.95f, 1.05f);
        
        // ── Routing ──────────────────────────────────────────────────────────────
        [Header("Routing")]
        [Tooltip("Which AudioMixerGroup this event routes to. Must match a group wired in AudioManager.")]
        [SerializeField] private MixerBus _bus = MixerBus.SFX;
        
        // ── Spatialization ──────────────────────────────────────────────────────────────
        [Header("Spatialization")]
        [Tooltip("False = flat 2D (UI, Music). True = positional - requires WorldPosition on play request.")]
        [SerializeField] private bool _is3D;
        
        [Tooltip("Distance at which the sound starts to fall off. Only used when Is3D is true.")]
        [SerializeField] private float _minDistance = 3f;
        
        [Tooltip("Distance at which the sound reaches zero volume. Only used when Is3D is true.")]
        [SerializeField] private float _maxDistance = 20f;
        
        [Tooltip("How the volume curve falls off over distance.")]
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;
        
        // ── Playback Behaviour ──────────────────────────────────────────────────────────────
        [Header("Playback Behaviour")]
        [Tooltip("Loop the clip. Use for music, ambience, or held SFX. Requires a tracked handle to stop.")]
        [SerializeField] private bool _loop;
        
        [Tooltip("Fade in duration in seconds when the sound starts. 0 = instant.")]
        [SerializeField] [Min(0f)] private float _fadeInDuration;
        
        [Tooltip("Fade out duration in seconds when the sound is stopped. 0 = instant.")]
        [SerializeField] [Min(0f)] private float _fadeOutDuration;
        
        // ── Polish / Polyphony ──────────────────────────────────────────────────────────────
        [Header("Polyphony & Cooldown")]
        [Tooltip("Maximum simultaneous instances of this specific event. 0 = unlimited. Voice stealing removes the oldest.")]
        [SerializeField] [Min(0f)] private int _polyphonyLimit;
        
        [Tooltip("Minimum seconds between plays of this event. Duplicate requests within this window are silently dropped. 0 = no cooldown.")]
        [SerializeField] [Min(0f)] private float _cooldownSeconds = 0f;
        

        #endregion

        #region Accessors and Variables

        // ── Public Accessors ──────────────────────────────────────────────────────────────
        public AudioClip[] Clips => _clips;
        public Vector2 VolumeRange => _volumeRange;
        public Vector2 PitchRange => _pitchRange;
        public MixerBus Bus => _bus;
        public bool Is3D => _is3D;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public AudioRolloffMode RolloffMode => _rolloffMode;
        public bool Loop => _loop;
        public float FadeInDuration => _fadeInDuration;
        public float FadeOutDuration => _fadeOutDuration;
        public int PolyphonyLimit => _polyphonyLimit;
        public float CooldownSeconds => _cooldownSeconds;
        

        #endregion
    }
}