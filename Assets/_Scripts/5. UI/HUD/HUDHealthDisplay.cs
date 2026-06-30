using Foundation;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace UI
{
    public sealed class HUDHealthDisplay : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Image _healthFillImage;

        [SerializeField] private Image _healthTrailImage;
        [SerializeField] private Image _heartsIcon;

        [Header("Configuration")] [SerializeField]
        private Sprite[] _heartSprites = new Sprite[4];

        [SerializeField] private float _mainLerpSpeed = 25f;
        [SerializeField] private float _trailLerpSpeed = 10f;

        [Header("Juice Configuration")] [SerializeField]
        private float _pulseScaleMultiplier = 1.3f;

        [SerializeField] private float _pulseDuration = 0.25f;

        private const float MIN_VISUAL_THRESHOLD = 0.02f;

        private VolatileRunState _activeRunState;
        private float _targetFillAmount;

        private float _lastKnownHp = -1f;
        private Vector3 _originalHeartScale;
        private Tween _pulseTween;

        private void Awake()
        {
            if (_heartsIcon != null)
            {
                _originalHeartScale = _heartsIcon.transform.localScale;
            }
        }

        private void OnEnable()
        {
            GameStateManager.OnRunStateInitialized += HandleRunStateInitialized;

            if (GameStateManager.RunState != null)
            {
                HandleRunStateInitialized(GameStateManager.RunState);
            }

            EventBus.Subscribe<PlayerDiedEvent>(HandlePlayerDied);
        }

        private void OnDisable()
        {
            GameStateManager.OnRunStateInitialized -= HandleRunStateInitialized;
            UnbindCurrentState();

            EventBus.Unsubscribe<PlayerDiedEvent>(HandlePlayerDied);

            _pulseTween?.Kill();
        }

        private void Update()
        {
            if (_healthFillImage != null && !Mathf.Approximately(_healthFillImage.fillAmount, _targetFillAmount))
            {
                _healthFillImage.fillAmount = Mathf.Lerp(_healthFillImage.fillAmount, _targetFillAmount,
                    Time.deltaTime * _mainLerpSpeed);

                if (Mathf.Abs(_healthFillImage.fillAmount - _targetFillAmount) < 0.01f)
                {
                    _healthFillImage.fillAmount = _targetFillAmount;
                }
            }

            if (_healthTrailImage != null && !Mathf.Approximately(_healthTrailImage.fillAmount, _targetFillAmount))
            {
                _healthTrailImage.fillAmount = Mathf.Lerp(_healthTrailImage.fillAmount, _targetFillAmount,
                    Time.deltaTime * _trailLerpSpeed);

                if (Mathf.Abs(_healthTrailImage.fillAmount - _targetFillAmount) < 0.01f)
                {
                    _healthTrailImage.fillAmount = _targetFillAmount;
                }
            }
        }

        private void HandleRunStateInitialized(VolatileRunState newState)
        {
            UnbindCurrentState();

            _activeRunState = newState;
            if (_activeRunState != null)
            {
                _activeRunState.OnHpChanged += UpdateUI;

                if (_activeRunState.MaxHp > 0)
                {
                    _lastKnownHp = _activeRunState.CurrentHp;

                    float initialNormalized = Mathf.Clamp01(_activeRunState.CurrentHp / _activeRunState.MaxHp);
                    _targetFillAmount = Mathf.Lerp(MIN_VISUAL_THRESHOLD, 1f, initialNormalized);

                    if (_healthFillImage != null) _healthFillImage.fillAmount = _targetFillAmount;
                    if (_healthTrailImage != null) _healthTrailImage.fillAmount = _targetFillAmount;

                    UpdateHeartSprite(initialNormalized);
                }
            }
        }

        private void UnbindCurrentState()
        {
            if (_activeRunState != null)
            {
                _activeRunState.OnHpChanged -= UpdateUI;
                _activeRunState = null;
            }
        }

        private void UpdateUI(float currentHp, float maxHp)
        {
            if (currentHp <= 0f || maxHp <= 0f)
            {
                _targetFillAmount = 0f;
                if (_heartsIcon != null && _heartsIcon.enabled) _heartsIcon.enabled = false;
                _lastKnownHp = currentHp;
                return;
            }

            if (_lastKnownHp > 0f && currentHp < _lastKnownHp)
            {
                PulseHeart();
            }

            _lastKnownHp = currentHp;

            float normalizedHealth = Mathf.Clamp01(currentHp / maxHp);

            if (_healthFillImage != null || _healthTrailImage != null)
            {
                _targetFillAmount = Mathf.Lerp(MIN_VISUAL_THRESHOLD, 1f, normalizedHealth);
            }

            UpdateHeartSprite(normalizedHealth);
        }

        private void PulseHeart()
        {
            if (_heartsIcon == null) return;

            _pulseTween?.Kill(true);

            _heartsIcon.transform.localScale = _originalHeartScale;

            _pulseTween = _heartsIcon.transform
                .DOScale(_originalHeartScale * _pulseScaleMultiplier, _pulseDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void HandlePlayerDied(PlayerDiedEvent evt)
        {
            _targetFillAmount = 0f;
            if (_heartsIcon != null && _heartsIcon.enabled) _heartsIcon.enabled = false;
        }

        private void ForceZeroVisibilityBaseline()
        {
            _targetFillAmount = 0f;
            if (_healthFillImage != null) _healthFillImage.fillAmount = 0f;
            if (_healthTrailImage != null) _healthTrailImage.fillAmount = 0f;

            if (_heartsIcon != null && _heartsIcon.enabled) _heartsIcon.enabled = false;
        }

        private void UpdateHeartSprite(float normalizedHealth)
        {
            if (_heartsIcon == null || _heartSprites.Length < 4) return;

            _heartsIcon.enabled = true;

            int maxIndex = _heartSprites.Length - 1;
            int spriteIndex = Mathf.FloorToInt(normalizedHealth * _heartSprites.Length);

            spriteIndex = Mathf.Clamp(spriteIndex, 0, maxIndex);
            _heartsIcon.sprite = _heartSprites[spriteIndex];
        }
    }
}