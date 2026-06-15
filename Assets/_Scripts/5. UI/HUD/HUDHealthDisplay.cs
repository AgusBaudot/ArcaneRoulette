using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class HUDHealthDisplay : MonoBehaviour
    {
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private Image _heartsIcon;
        [SerializeField] private Sprite[] _heartSprites = new Sprite[4];

        private VolatileRunState _activeRunState;

        private void OnEnable()
        {
            GameStateManager.OnRunStateInitialized += HandleRunStateInitialized;
            
            if (GameStateManager.RunState != null)
            {
                HandleRunStateInitialized(GameStateManager.RunState);
            }
        }

        private void OnDisable()
        {
            GameStateManager.OnRunStateInitialized -= HandleRunStateInitialized;
            UnbindCurrentState();
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
            }
        }

        private void Update()
        {
            // Test input
            if (Input.GetKeyDown(KeyCode.Space) && _activeRunState != null)
            {
                _activeRunState.SetHp(_activeRunState.CurrentHp - 10f);
            }
        }

        // BULLETPROOF FALLBACK: Evaluates state every single frame after physics and gameplay logic
        private void LateUpdate()
        {
            // Try global access first, fallback to cached instance
            var currentRunState = GameStateManager.RunState ?? _activeRunState;
            
            if (currentRunState != null && currentRunState.CurrentHp <= 0f)
            {
                ForceZeroVisibilityBaseline();
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
                // Mínimo visible: 1% de la barra
                const float minVisibleFill = 0.01f;
                _healthFillImage.fillAmount = Mathf.Max(normalizedHealth, minVisibleFill);
            }

            UpdateHeartSprite(normalizedHealth);
    

        }

        private void ForceZeroVisibilityBaseline()
        {
            if (_healthFillImage != null) _healthFillImage.fillAmount = 0f;
            if (_heartsIcon != null && _heartsIcon.enabled) 
            {
                _heartsIcon.enabled = false; // Drops down layer instantly
            }
        }

        private void UpdateHeartSprite(float normalizedHealth)
        {
            if (_heartsIcon == null || _heartSprites.Length < 4)
                return;

            _heartsIcon.enabled = true;
            
            int spriteIndex = Mathf.FloorToInt(normalizedHealth * 4f);
            spriteIndex = Mathf.Clamp(spriteIndex, 0, 3);
            _heartsIcon.sprite = _heartSprites[spriteIndex];
        }
    }
}
