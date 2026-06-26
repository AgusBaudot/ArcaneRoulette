using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class HUDHealthDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private Image _heartsIcon;

        [Header("Configuration")]
        [SerializeField] private Sprite[] _heartSprites = new Sprite[4];
        [SerializeField] private float _lerpSpeed = 5f; // Added speed control for the interpolation

        private const float MIN_VISUAL_THRESHOLD = 0.02f;

        private VolatileRunState _activeRunState;

        // Tracks where the health bar SHOULD be, so Update() can smoothly move towards it
        private float _targetFillAmount;

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
        }

        private void Update()
        {
            // Smoothly interpolate the fill amount towards the target value every frame
            if (_healthFillImage != null && _healthFillImage.fillAmount != _targetFillAmount)
            {
                _healthFillImage.fillAmount = Mathf.Lerp(_healthFillImage.fillAmount, _targetFillAmount, Time.deltaTime * _lerpSpeed);

                // Snap to target if it gets extremely close to prevent endless micro-calculations
                if (Mathf.Abs(_healthFillImage.fillAmount - _targetFillAmount) < 0.001f)
                {
                    _healthFillImage.fillAmount = _targetFillAmount;
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

                // Snap the visual instantly when initializing, so it doesn't "fill up" from 0 on spawn
                if (_activeRunState.MaxHp > 0)
                {
                    float initialNormalized = Mathf.Clamp01(_activeRunState.CurrentHp / _activeRunState.MaxHp);
                    _targetFillAmount = Mathf.Lerp(MIN_VISUAL_THRESHOLD, 1f, initialNormalized);

                    if (_healthFillImage != null)
                    {
                        _healthFillImage.fillAmount = _targetFillAmount;
                    }
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
                // Let the Update() loop empty the bar smoothly, but instantly hide the heart icon
                _targetFillAmount = 0f;
                if (_heartsIcon != null && _heartsIcon.enabled)
                {
                    _heartsIcon.enabled = false;
                }
                return;
            }

            float normalizedHealth = Mathf.Clamp01(currentHp / maxHp);

            // Set the new target instead of applying it instantly
            if (_healthFillImage != null)
            {
                _targetFillAmount = Mathf.Lerp(MIN_VISUAL_THRESHOLD, 1f, normalizedHealth);
            }

            UpdateHeartSprite(normalizedHealth);
        }

        private void HandlePlayerDied(PlayerDiedEvent evt)
        {
            // Usually upon death, we want it to deplete smoothly, so we just set the target to 0
            _targetFillAmount = 0f;
            if (_heartsIcon != null && _heartsIcon.enabled)
            {
                _heartsIcon.enabled = false;
            }
        }

        private void ForceZeroVisibilityBaseline()
        {
            // This remains as a hard reset for when we absolutely need the UI to instantly blank out
            _targetFillAmount = 0f;
            if (_healthFillImage != null)
            {
                _healthFillImage.fillAmount = 0f;
            }

            if (_heartsIcon != null && _heartsIcon.enabled)
            {
                _heartsIcon.enabled = false;
            }
        }

        private void UpdateHeartSprite(float normalizedHealth)
        {
            if (_heartsIcon == null || _heartSprites.Length < 4)
                return;

            _heartsIcon.enabled = true;

            int maxIndex = _heartSprites.Length - 1;
            int spriteIndex = Mathf.FloorToInt(normalizedHealth * _heartSprites.Length);

            spriteIndex = Mathf.Clamp(spriteIndex, 0, maxIndex);
            _heartsIcon.sprite = _heartSprites[spriteIndex];
        }
    }
}