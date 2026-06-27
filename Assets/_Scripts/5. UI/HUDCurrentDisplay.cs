using Foundation;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace UI
{
    public sealed class HUDCurrencyDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _currencyText;
        [SerializeField] private TextMeshProUGUI _deltaText; // The floating +/- text
        [SerializeField] private RectTransform _crystalIcon;

        private int _currentDisplayedValue;

        private void Awake()
        {
            _deltaText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameStateManager.RunState.OnCurrencyChanged += UpdateDisplay;
            _currentDisplayedValue = GameStateManager.RunState.Currency;
            _currencyText.text = _currentDisplayedValue.ToString();
        }

        private void OnDisable()
        {
            if (GameStateManager.RunState != null)
                GameStateManager.RunState.OnCurrencyChanged -= UpdateDisplay;
        }

        private void UpdateDisplay(int newValue)
        {
            int delta = newValue - _currentDisplayedValue;
            if (delta == 0) return;

            _currentDisplayedValue = newValue;
            _currencyText.text = newValue.ToString();

            // Handle Delta Text (+10 or -40)
            _deltaText.text = delta > 0 ? $"+{delta}" : $"{delta}";
            _deltaText.color = delta > 0 ? Color.green : Color.red;
            _deltaText.gameObject.SetActive(true);
            _deltaText.rectTransform.anchoredPosition = Vector2.zero; // Reset pos

            // Punch scale and float text (SetUpdate(true) ensures it works if game is paused)
            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(_crystalIcon.DOPunchScale(Vector3.one * 1f, 0.3f, 5, 1f));
            seq.Join(_deltaText.rectTransform.DOAnchorPosY(30f, 0.5f).SetEase(Ease.OutCubic));
            seq.Join(_deltaText.DOFade(0f, 0.5f).SetDelay(0.5f));
            seq.OnComplete(() =>
            {
                _deltaText.gameObject.SetActive(false);
                _deltaText.color =
                    new Color(_deltaText.color.r, _deltaText.color.g, _deltaText.color.b, 1f); // Reset alpha
            });
        }
    }
}