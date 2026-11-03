using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using Foundation;

namespace Core
{
    public class DeathScreenFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Volume _deathVolume;

        [Header("Gray fade")]
        [Tooltip("Unscaled seconds to wait after the death event before the gray starts.")]
        [SerializeField, Min(0f)] private float _startDelay = 0.1f;
        [SerializeField, Min(0.01f)] private float _fadeInDuration = 1f;
        [SerializeField] private Ease _fadeInEase = Ease.OutCubic;

        [Header("Slow-mo")]
        [Tooltip("Time scale the instant the player dies.")]
        [SerializeField, Range(0f, 1f)] private float _initialTimeScale = 0.3f;
        [Tooltip("Where it settles. 0 = frozen until the next scene load resets it.")]
        [SerializeField, Range(0f, 1f)] private float _endTimeScale = 0f;
        [SerializeField, Min(0.01f)] private float _slowMoDuration = 1.5f;
        [SerializeField] private Ease _slowMoEase = Ease.OutQuad;

        [Header("Revive")]
        [SerializeField, Min(0.01f)] private float _fadeOutDuration = 0.25f;

        private Tween _grayTween;
        private Tween _timeTween;
        private bool _isDead;

        private void Awake()
        {
            HitStop.Unlock();
            ResetVolume();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<PlayerRevivedEvent>(OnPlayerRevived);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<PlayerRevivedEvent>(OnPlayerRevived);

            _grayTween?.Kill();
            _timeTween?.Kill();
            _isDead = false;

            HitStop.Unlock();
            ResetVolume();
        }

        private void OnPlayerDied(PlayerDiedEvent _)
        {
            if (_isDead) return;
            _isDead = true;

            HitStop.CancelAndLock();

            Time.timeScale = _initialTimeScale;
            _timeTween?.Kill();
            _timeTween = DOTween.To(() => Time.timeScale, v => Time.timeScale = v, _endTimeScale, _slowMoDuration)
                .SetEase(_slowMoEase)
                .SetUpdate(true)
                .SetTarget(this);

            FadeGray(1f, _fadeInDuration, _fadeInEase, _startDelay);
        }

        private void OnPlayerRevived(PlayerRevivedEvent _)
        {
            if (!_isDead) return;
            _isDead = false;

            _timeTween?.Kill();
            HitStop.Unlock();
            Time.timeScale = 1f;

            FadeGray(0f, _fadeOutDuration, Ease.OutQuad, 0f);
        }

        private void FadeGray(float target, float duration, Ease ease, float delay)
        {
            if (_deathVolume == null) return;

            _grayTween?.Kill();
            _grayTween = DOTween.To(() => _deathVolume.weight, w => _deathVolume.weight = w, target, duration)
                .SetDelay(delay)
                .SetEase(ease)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void ResetVolume()
        {
            if (_deathVolume != null)
                _deathVolume.weight = 0f;
        }
    }
}