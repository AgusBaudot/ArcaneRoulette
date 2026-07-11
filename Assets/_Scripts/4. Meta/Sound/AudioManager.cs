using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.Audio;

namespace Meta
{
    /// <summary>
    /// Central audio system. Subscribes to all audio EventBus payloads.
    /// Gameplay code never calls this directly - it publishes requests to the EventBus.
    ///
    /// Responsibilities:
    /// - Manages a pool of AudioEmitter GameObject (pooled AudioSource).
    /// - Routes each AudioEventSO to the correct AudioMixerGroup.
    /// - Enforces polyphony limits (voice stealing) and micro-cooldowns.
    /// - Handles music crossfading via two dedicated emitter slots (A/B).
    /// - Handles audio ducking via AnimationCurve-tweened mixer parameters.
    /// - Persists volume preferences to PlayerPrefs.
    ///
    /// Bootstrapper auto-injects this into isolated scenes.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        #region Properties

        // ── Singleton ──────────────────────────────────────────────────────────
        public static AudioManager Instance { get; private set; }
        
        // ── Mixer ──────────────────────────────────────────────────────────
        [Header("Mixer Groups")]
        [Tooltip("Wire each AudioMixerGroup from our project mixer asset.")]
        [SerializeField] private AudioMixer _masterMixer;
        [SerializeField] private AudioMixerGroup _masterGroup;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private AudioMixerGroup _ambienceGroup;
        
        //Exposed parameter names must match our AudioMixer's exposed parameters exactly.
        //Right-click a parameter in the AudioMixer inspector -> "Expose" -> rename to these:
        [Header("Mixer Exposed Parameter Names")]
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _musicVolumeParam = "MusicVolume";
        [SerializeField] private string _sfxVolumeParam = "SFXVolume";
        [SerializeField] private string _uiVolumeParam = "UIVolume";
        [SerializeField] private string _ambienceVolumeParam = "AmbienceVolume";
        
        // ── Pool ───────────────────────────────────────────────────────────────
        [Header("Emitter Pool")]
        [Tooltip("Number of AudioEmitter GameObjects pre-allocated at startup.")]
        [SerializeField] [Min(8)] private int _poolInitialCapacity = 32;
        [Tooltip("Hard cap on pool size. Requests that would exceed this are dropped with a warning.")]
        [SerializeField] [Min(16)] private int _poolMaxSize = 64;
        
        // ── Ducking ────────────────────────────────────────────────────────────
        [Header("Ducking")]
        [Tooltip("Curve shape for the duck-down ramp (x=normalized time, y=normalized volume multiplier).")]
        [SerializeField] private AnimationCurve _duckAttackCurve = AnimationCurve.EaseInOut(0,1,1,0);
        [Tooltip("Curve shape for the duck-release ramp back to full.")]
        [SerializeField] private AnimationCurve _duckReleaseCurve = AnimationCurve.EaseInOut(0,0,1,1);
        
        // ── Internal Pool State ────────────────────────────────────────────────
        private readonly Queue<AudioEmitter> _pool = new();
        private readonly List<AudioEmitter> _activeEmitters = new();
        
        //Per-event tracking for polyphony limits
        private readonly Dictionary<AudioEventSO, List<AudioEmitter>> _activeByEvent = new();
        
        //Per-event cooldown tracking (uses unscaled time)
        private readonly Dictionary<AudioEventSO, float> _lastPlayTime = new();
        
        //Per-event clip index tracking
        private readonly Dictionary<AudioEventSO, int> _lastPlayedClipIndex = new();
        
        // ── Music Crossfade State ──────────────────────────────────────────────
        private AudioEmitter _musicSlotA;
        private AudioEmitter _musicSlotB;
        private bool _musicSlotAActive; //which slot is currently "main"
        private Coroutine _crossfadeRoutine;
        
        // ── Duck State ─────────────────────────────────────────────────────────
        //Tracks the deepest active duck per bus so concurrent requests don't fight.
        private readonly Dictionary<MixerBus, float> _activeDuckDepths = new();
        private readonly Dictionary<MixerBus, Coroutine> _duckCoroutines = new();
        
        // ── PlayerPrefs Keys ───────────────────────────────────────────────────
        private const string PrefMaster = "Audio_Master";
        private const string PrefMusic = "Audio_Music";
        private const string PrefSFX = "Audio_SFX";
        private const string PrefUI = "Audio_UI";
        private const string PrefAmbience = "Audio_Ambience";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildPool();
        }

        private void Start()
        {
            LoadVolumePrefs();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<AudioPlayRequest>(OnPlayRequest);
            EventBus.Subscribe<AudioPlayTrackedRequest>(OnPlayTrackedRequest);
            EventBus.Subscribe<AudioStopRequest>(OnStopRequest);
            EventBus.Subscribe<AudioCrossfadeRequest>(OnCrosssfadeRequest);
            EventBus.Subscribe<AudioDuckRequest>(OnDuckRequest);
            EventBus.Subscribe<AudioModulateRequest>(OnModulateRequest);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AudioPlayRequest>(OnPlayRequest);
            EventBus.Unsubscribe<AudioPlayTrackedRequest>(OnPlayTrackedRequest);
            EventBus.Unsubscribe<AudioStopRequest>(OnStopRequest);
            EventBus.Unsubscribe<AudioCrossfadeRequest>(OnCrosssfadeRequest);
            EventBus.Unsubscribe<AudioDuckRequest>(OnDuckRequest);
            EventBus.Unsubscribe<AudioModulateRequest>(OnModulateRequest);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region EventBus Handlers

        private void OnPlayRequest(AudioPlayRequest req)
        {
            if (req.Event == null)
                return;

            Play(req.Event, req.WorldPosition, handle: null);
        }

        private void OnPlayTrackedRequest(AudioPlayTrackedRequest req)
        {
            if (req.Event == null)
                return;

            var handle = new AudioHandle();
            Play(req.Event, req.WorldPosition, handle);
            req.OnHandleReady?.Invoke(handle);
        }

        private void OnStopRequest(AudioStopRequest req)
        {
            if (req.Handle == null || !req.Handle.IsValid)
                return;

            var emitter = req.Handle.Emitter;
            if (emitter == null)
                return;
            
            if (req.FadeOut && req.Handle.Event != null && req.Handle.Event.FadeOutDuration > 0f)
                emitter.StopWithFade(req.Handle.Event.FadeOutDuration);
            else
                emitter.StopImmediate();
        }

        private void OnCrosssfadeRequest(AudioCrossfadeRequest req)
        {
            if (_crossfadeRoutine != null)
                StopCoroutine(_crossfadeRoutine);

            _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(req.NewTrack, req.Duration));
        }

        private void OnDuckRequest(AudioDuckRequest req)
        {
            //If already ducking this bus, take the deeper duck.
            float incoming = req.DuckToVolume;
            if (_activeDuckDepths.TryGetValue(req.TargetBus, out float current))
            {
                if (current <= incoming)
                    return;
            }

            _activeDuckDepths[req.TargetBus] = incoming;

            if (_duckCoroutines.TryGetValue(req.TargetBus, out var existing) && existing != null)
                StopCoroutine(existing);

            _duckCoroutines[req.TargetBus] = StartCoroutine(DuckRoutine(req));
        }

        private void OnModulateRequest(AudioModulateRequest req)
        {
            if (req.Handle == null || !req.Handle.IsValid)
                return;

            var emitter = req.Handle.Emitter;
            if (emitter == null)
                return;
            
            if (req.Pitch.HasValue)
                emitter.SetPitch(req.Pitch.Value);
            
            if (req.Volume.HasValue)
                emitter.SetVolume(req.Volume.Value);
        }

        #endregion

        #region Core Play Logic

        private void Play(AudioEventSO audioEvent, Vector3 worldPos, AudioHandle handle)
        {
            // ── Micro-cooldown check ───────────────────────────────────────────
            if (audioEvent.CooldownSeconds > 0f)
            {
                if (_lastPlayTime.TryGetValue(audioEvent, out float last))
                {
                    if (Time.unscaledTime - last > audioEvent.CooldownSeconds)
                        return;
                }
                
                _lastPlayTime[audioEvent] = Time.unscaledTime; 
            }
            
            // ── Polyphony / voice stealing ─────────────────────────────────────
            if (audioEvent.PolyphonyLimit > 0)
            {
                if (!_activeByEvent.TryGetValue(audioEvent, out var actives))
                {
                    actives = new List<AudioEmitter>();
                    _activeByEvent[audioEvent] = actives;
                }

                if (actives.Count >= audioEvent.PolyphonyLimit)
                {
                    //Steal the oldest active instance.
                    var oldest = actives[0];
                    oldest.StopImmediate(); //triggers OnNaturalEnd -> ReturnToPool -> removes from list
                }
            }
            
            // ── Pool checkout ──────────────────────────────────────────────────
            var emitter = CheckoutEmitter();
            if (emitter == null) 
                return;
            
            //Wire handle and emitter (handle.Emitter is typed as object to keep Foundation clean)
            if (handle != null)
                handle.Emitter = emitter;
            
            // ── Track per-event ────────────────────────────────────────────────
            if (!_activeByEvent.TryGetValue(audioEvent, out var list))
            {
                list = new List<AudioEmitter>();
                _activeByEvent[audioEvent] = list;
            }
            
            list.Add(emitter);
            _activeEmitters.Add(emitter);

            emitter.gameObject.SetActive(true);
            emitter.Init(
                audioEvent,
                GetNextClip(audioEvent),
                GetMixerGroup(audioEvent.Bus),
                worldPos,
                handle,
                onNaturalEnd: e => ReturnToPool(audioEvent, e)
            );
        }
        
        /// <summary>
        /// Resolves the next clip to play, guaranteeing no consecutive repeats 
        /// if multiple clips are available.
        /// </summary>
        private AudioClip GetNextClip(AudioEventSO audioEvent)
        {
            if (audioEvent.Clips == null || audioEvent.Clips.Length == 0)
            {
                Debug.LogWarning($"[AudioManager] AudioEvent '{audioEvent.name}' has no clips assigned.");
                return null;
            }

            if (audioEvent.Clips.Length == 1)
                return audioEvent.Clips[0];

            int lastIndex = _lastPlayedClipIndex.GetValueOrDefault(audioEvent, -1);
            int newIndex;

            do
            {
                newIndex = Random.Range(0, audioEvent.Clips.Length);
            } 
            while (newIndex == lastIndex);

            _lastPlayedClipIndex[audioEvent] = newIndex;
            return audioEvent.Clips[newIndex];
        }

        #endregion

        #region Music Crossfade

        private IEnumerator CrossfadeRoutine(AudioEventSO newTrack, float duration)
        {
            //Determine which slot is outgoing and which is incoming.
            AudioEmitter outgoing = _musicSlotAActive ? _musicSlotA : _musicSlotB;
            AudioEmitter incoming = _musicSlotAActive ? _musicSlotB : _musicSlotA;
            
            //Ensure the incoming slot is clean.
            incoming?.StopImmediate();

            if (newTrack != null)
            {
                //Spawn onto the inactive slot directly (bypass polyphony - music is special).
                var nextEmitter = CheckoutEmitter();
                if (nextEmitter == null)
                {
                    _crossfadeRoutine = null;
                    yield break;
                }
                
                if (_musicSlotAActive)
                    _musicSlotB = nextEmitter;
                else
                    _musicSlotA = nextEmitter;

                nextEmitter.gameObject.SetActive(true);
                nextEmitter.Init(newTrack, GetNextClip(newTrack), GetMixerGroup(MixerBus.Music), Vector3.zero, null,
                    onNaturalEnd: e => ReturnToPool(newTrack, e));
                
                //Immediately set to 0 volume; we'll fade it in.
                nextEmitter.SetVolume(0f);

                float elapsed = 0f;
                float outStartVol = outgoing != null ? 1f : 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    outgoing?.SetVolume(Mathf.Lerp(outStartVol, 0f, t));
                    nextEmitter.SetVolume(Mathf.Lerp(0f, 1f, t));
                    yield return null;
                }
                
                outgoing?.StopImmediate();
                nextEmitter.SetVolume(1f);
            }
            else
            {
                //Fade out with no replacement.
                if (outgoing != null)
                    outgoing.StopWithFade(duration > 0f ? duration : 0f);
            }

            _musicSlotAActive = !_musicSlotAActive;
            _crossfadeRoutine = null;
        }

        #endregion

        #region Ducking

        private IEnumerator DuckRoutine(AudioDuckRequest req)
        {
            string param = GetVolumeParam(req.TargetBus);
            
            //Attack - ramp down
            float elapsed = 0f;
            while (elapsed < req.AttackSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / req.AttackSeconds);
                float v = Mathf.Lerp(1f, req.DuckToVolume, _duckAttackCurve.Evaluate(t));
                SetMixerVolume(param, v);
                yield return null;
            }

            SetMixerVolume(param, req.DuckToVolume);
            
            //Hold
            float holdElapsed = 0f;
            while (holdElapsed < req.HoldSeconds)
            {
                holdElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            //Release - ramp back up
            elapsed = 0f;
            while (elapsed < req.ReleaseSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / req.ReleaseSeconds);
                float v = Mathf.Lerp(req.DuckToVolume, 1f, _duckReleaseCurve.Evaluate(t));
                SetMixerVolume(param, v);
                yield return null;
            }

            SetMixerVolume(param, 1f);

            _activeDuckDepths.Remove(req.TargetBus);
            _duckCoroutines.Remove(req.TargetBus);
        }

        #endregion

        #region Volume Settings (called from Settings UI)

        /// <summary>
        /// Set the volume for a bus. normalizedVolume is 0-1.
        /// Persists to PlayerPrefs. Call from the setting UI sliders.
        /// </summary>
        public void SetBusVolume(MixerBus bus, float normalizedVolume)
        {
            string param = GetVolumeParam(bus);
            string pref = GetPrefKey(bus);
            SetMixerVolume(param, normalizedVolume);
            PlayerPrefs.SetFloat(pref, normalizedVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Returns the current normalised volume (0-1) for a bus.
        /// </summary>
        public float GetBusVolume(MixerBus bus)
            => PlayerPrefs.GetFloat(GetPrefKey(bus), 1f);

        private void LoadVolumePrefs()
        {
            SetMixerVolume(_masterVolumeParam, PlayerPrefs.GetFloat(PrefMaster, 1f));
            SetMixerVolume(_musicVolumeParam, PlayerPrefs.GetFloat(PrefMusic, 1f));
            SetMixerVolume(_sfxVolumeParam, PlayerPrefs.GetFloat(PrefSFX, 1f));
            SetMixerVolume(_uiVolumeParam, PlayerPrefs.GetFloat(PrefUI, 1f));
            SetMixerVolume(_ambienceVolumeParam, PlayerPrefs.GetFloat(PrefAmbience, 1f));
        }
        
        //AudioMixer uses decibels; we expose normalized 0-1 to the UI and convert here.
        //0 volume -> -80 dB (effectively silent). 1 -> 0 dB (full).
        private void SetMixerVolume(string paramName, float normalized)
        {
            if (_masterMixer == null)
                return;
            
            float db = normalized > 0.0001f ? 20f * Mathf.Log10(normalized) : -80f;
            _masterMixer.SetFloat(paramName, db);
        }

        #endregion

        #region Pool Management

        private void BuildPool()
        {
            var container = new GameObject("AudioEmitters");
            container.transform.SetParent(transform);
            
            for (int i = 0; i < _poolInitialCapacity; i++)
                _pool.Enqueue(CreateEmitter(container.transform));
        }

        private AudioEmitter CreateEmitter(Transform parent)
        {
            var go = new GameObject("AudioEmitter");
            go.transform.SetParent(parent);
            go.SetActive(false);
            return go.AddComponent<AudioEmitter>();
        }

        private AudioEmitter CheckoutEmitter()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();
            
            //Grow pool if under hard cap.
            if (_activeEmitters.Count < _poolMaxSize)
            {
                var newEmitter = CreateEmitter(transform);
                return newEmitter;
            }
            
            Debug.LogWarning("[AudioManager] Pool exhausted - audio request dropped. Consider raising _poolMaxSize.");
            return null;
        }

        private void ReturnToPool(AudioEventSO audioEvent, AudioEmitter emitter)
        {
            //Remove from per-event tracking.
            if (audioEvent != null && _activeByEvent.TryGetValue(audioEvent, out var list))
                list.Remove(emitter);

            _activeEmitters.Remove(emitter);
            
            emitter.ResetForPool();
            _pool.Enqueue(emitter);
        }

        #endregion

        #region Helpers

        private AudioMixerGroup GetMixerGroup(MixerBus bus)
            => bus switch
            {
                MixerBus.Music => _musicGroup,
                MixerBus.SFX => _sfxGroup,
                MixerBus.UI => _uiGroup,
                MixerBus.Ambience => _ambienceGroup,
                _ => _masterGroup
            };

        private string GetVolumeParam(MixerBus bus)
            => bus switch
            {
                MixerBus.Music => _musicVolumeParam,
                MixerBus.SFX => _sfxVolumeParam,
                MixerBus.UI => _uiVolumeParam,
                MixerBus.Ambience => _ambienceVolumeParam,
                _ => _masterVolumeParam
            };

        private static string GetPrefKey(MixerBus bus)
            => bus switch
            {
                MixerBus.Music => PrefMusic,
                MixerBus.SFX => PrefSFX,
                MixerBus.UI => PrefUI,
                MixerBus.Ambience => PrefAmbience,
                _ => PrefMaster
            };

        #endregion
    }
}