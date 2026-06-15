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

        private void OnEnable()
        {
            GameStateManager.RunState.OnHpChanged += UpdateUI;

            UpdateUI(GameStateManager.RunState.CurrentHp, GameStateManager.RunState.MaxHp);
        }

        private void OnDisable()
        {
            GameStateManager.RunState.OnHpChanged -= UpdateUI;
        }

        private void Update()
        {
            // Test input: Press Space to damage player by 10 HP
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameStateManager.RunState.SetHp(GameStateManager.RunState.CurrentHp - 10f);
            }
        }

        private void UpdateUI(float currentHp, float maxHp)
        {
            float normalizedHealth = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

            if (_healthFillImage != null)
            {
                _healthFillImage.fillAmount = normalizedHealth;
            }

            UpdateHeartSprite(currentHp, maxHp);
        }

        private void UpdateHeartSprite(float currentHp, float maxHp)
        {
            if (_heartsIcon == null || _heartSprites.Length < 4)
                return;

            float normalizedHealth = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            
            if (normalizedHealth <= 0f)
            {
                _heartsIcon.enabled = false;
                return;
            }

            _heartsIcon.enabled = true;
            int spriteIndex = Mathf.FloorToInt(normalizedHealth * 4f);
            spriteIndex = Mathf.Clamp(spriteIndex, 0, 3);
            _heartsIcon.sprite = _heartSprites[spriteIndex];
        }

    }
}
