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

        private const float MIN_VISUAL_THRESHOLD = 0.02f;
        
        private VolatileRunState _activeRunState;

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

        private void HandleRunStateInitialized(VolatileRunState newState)
        {
            UnbindCurrentState();
            
            _activeRunState = newState;
            if (_activeRunState != null)
            {
                _activeRunState.OnHpChanged += UpdateUI;
                UpdateUI(_activeRunState.CurrentHp, _activeRunState.MaxHp);
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
                ForceZeroVisibilityBaseline();
                return;
            }

            float normalizedHealth = Mathf.Clamp01(currentHp / maxHp);
            
            if (_healthFillImage != null)
            {
                _healthFillImage.fillAmount = Mathf.Lerp(MIN_VISUAL_THRESHOLD, 1f, normalizedHealth);
            }

            UpdateHeartSprite(normalizedHealth);
        }

        private void HandlePlayerDied(PlayerDiedEvent evt)
        {
            ForceZeroVisibilityBaseline();
        }

        private void ForceZeroVisibilityBaseline()
        {
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
