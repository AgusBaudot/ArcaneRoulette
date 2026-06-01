using System.Collections;
using Foundation;
using UnityEngine;
using UnityEngine.Audio;

namespace Meta
{
    /// <summary>
    /// Internal pooled component - one AudioSource per emitter.
    /// Never reference this class from Core or World. All interaction is via AudioHandle.
    ///
    /// Lifecycle: AudioManager.Spawn -> Init (Play/Fade) -> Despawn -> pool reset.
    /// The AudioManager owns the pool; emitters never despawn themselves except via
    /// the natural end path (_naturalEndRoutine), which calls back to the manager.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    internal sealed class AudioEmitter : MonoBehaviour
    {
        // ── Components ───────────────────────────────────────────────────
        private AudioSource _source;
        
        // ── State ────────────────────────────────────────────────────────
        private AudioHandle _handle;
        private Coroutine _fadeRoutine;
        private Coroutine _naturalEndRoutine;
        
        //Callback to AudioManager so it can remove this emitter from its tracking
        //dictionaries and return it to the pool on natural end.
        private System.Action<AudioEmitter> _onNaturalEnd;
        
        // ── Init ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        /// <summary>
        /// Called by AudioManager immediately after pulling from pool.
        /// Configures the AudioSource and begins playback.
        /// </summary>
        internal void Init(
            AudioEventSO audioEvent,
            AudioMixerGroup mixerGroup,
            Vector3 worldPosition,
            AudioHandle handle,
            System.Action<AudioEmitter> onNaturalEnd)
        {
            _handle = handle;
            _onNaturalEnd = onNaturalEnd;
            
            // ── Position ─────────────────────────────────────────────────────
            transform.position = audioEvent.Is3D ? worldPosition : Vector3.zero;
            
            // ── AudioSource configuration ────────────────────────────────────
            _source.clip = audioEvent.PickClip();
            _source.outputAudioMixerGroup = mixerGroup;
            _source.loop = audioEvent.Loop;
            _source.spatialBlend = audioEvent.Is3D ? 1f : 0f;
            _source.minDistance = audioEvent.MinDistance;
            _source.maxDistance = audioEvent.MaxDistance;
            _source.rolloffMode = audioEvent.RolloffMode;
            _source.pitch = Random.Range(audioEvent.PitchRange.x, audioEvent.PitchRange.y);
            
            float targetVolume = Random.Range(audioEvent.VolumeRange.x, audioEvent.VolumeRange.y);

            if (_source.clip == null)
            {
                //PickClip already logged a warning; bail cleanly.
                onNaturalEnd?.Invoke(this);
                return;
            }
            
            // ── Fade-in or instant start ─────────────────────────────────────
            if (audioEvent.FadeInDuration > 0f)
            {
                _source.volume = 0f;
                _source.Play();
                _fadeRoutine = StartCoroutine(FadeVolume(0f, targetVolume, audioEvent.FadeInDuration));
            }
            else
            {
                _source.volume = targetVolume;
                _source.Play();
            }
            
            // ── Natural-end watcher (non-looping sounds only) ────────────────
            if (!audioEvent.Loop)
                _naturalEndRoutine = StartCoroutine(WatchForNaturalEnd());

            if (_handle != null)
            {
                _handle.IsPlaying = true;
                _handle.IsValid = true;
                _handle.Event = audioEvent;
            }
        }
        
        // ── Public Controls (called by AudioManager only) ────────────────────
        
        /// <summary>
        /// Stop immediately and signal natural-end so the manager can return to pool.
        /// </summary>
        internal void StopImmediate()
        {
            StopAllCoroutines();
            _source.Stop();
            _handle?.Invalidate();
            _onNaturalEnd?.Invoke(this);
        }

        /// <summary>
        /// Fade out over duration, then signal end.
        /// </summary>
        /// <param name="duration"></param>
        internal void StopWithFade(float duration)
        {
            StopAllCoroutines();
            if (duration <= 0f)
            {
                StopImmediate();
                return;
            }
            
            _fadeRoutine = StartCoroutine(FadeOutAndStop(duration));
        }

        /// <summary>
        /// Directly set pitch. For real-time modulation (engine hum, etc.)
        /// </summary>
        internal void SetPitch(float pitch) => _source.pitch = pitch;
        
        /// <summary>
        /// Directly set volume. For real-time modulation.
        /// </summary>
        internal void SetVolume(float volume) => _source.volume = volume;
        
        internal bool IsPlaying => _source.isPlaying;
        
        // ── Pool Reset ─────────────────────────────────────────────────────────

        /// <summary>
        /// Reset all mutable state before returning to pool.
        /// Mirrors the IPoolable.OnDespawn protocol used by BaseProjectile.
        /// </summary>
        internal void ResetForPool()
        {
            StopAllCoroutines();
            _fadeRoutine = null;
            _naturalEndRoutine = null;
            
            _source.Stop();
            _source.clip = null;
            _source.loop = false;
            _source.volume = 1f;
            _source.pitch = 1f;
            _source.spatialBlend = 0f;
            _source.outputAudioMixerGroup = null;
            _source.ignoreListenerPause = false;
            
            _handle?.Invalidate();
            _handle = null;
            _onNaturalEnd = null;

            gameObject.SetActive(false);
        }
        
        // ── Coroutines ─────────────────────────────────────────────────────────

        private IEnumerator WatchForNaturalEnd()
        {
            //Wait one frame for Play() to register, then poll until done.
            yield return null;
            while (_source.isPlaying)
                yield return null;
            
            _handle?.Invalidate();
            _onNaturalEnd?.Invoke(this);
        }

        private IEnumerator FadeVolume(float from, float to, float duration)
        {
            float elapsed = 0f;
            _source.volume = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _source.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _source.volume = to;
            _fadeRoutine = null;
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            float startVolume = _source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            
            _source.volume = 0f;
            _source.Stop();
            _handle?.Invalidate();
            _onNaturalEnd?.Invoke(this);
        }
    }
}