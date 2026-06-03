using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class HUDHealthDisplay : MonoBehaviour
    {
        [SerializeField] private RectMask2D _healthFillMask;
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

        private void UpdateUI(float currentHp, float maxHp)
        {
            float normalizedHealth = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            float maskWidth = _healthFillMask.GetComponent<RectTransform>().rect.width;

            int fillWidth = Mathf.RoundToInt(normalizedHealth * maskWidth);
            if (currentHp > 0f && fillWidth == 0 && maskWidth >= 1f)
                fillWidth = 1;

            int rightPadding = Mathf.RoundToInt(maskWidth) - fillWidth;
            _healthFillMask.padding = new Vector4(0f, 0f, rightPadding, 0f);

            UpdateHeartSprite(currentHp, maxHp);
        }

        private void UpdateHeartSprite(float currentHp, float maxHp)
        {
            if (_heartsIcon == null || _heartSprites.Length < 4)
                return;

            float normalizedHealth = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            int spriteIndex = Mathf.FloorToInt(normalizedHealth * 4f);
            spriteIndex = Mathf.Clamp(spriteIndex, 0, 3);
            _heartsIcon.sprite = _heartSprites[spriteIndex];
        }

    }
}
