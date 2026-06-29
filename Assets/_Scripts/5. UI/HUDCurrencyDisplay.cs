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
        private Tweener _counterTween;
        private Sequence _juiceSequence;
        private Vector3 _baseIconScale;

        private void Awake()
        {
            _deltaText.gameObject.SetActive(false);

            if (_crystalIcon != null)
            {
                _baseIconScale = _crystalIcon.localScale;
            }
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
            
            _counterTween?.Kill();
            _juiceSequence?.Kill();
        }

        private void UpdateDisplay(int newValue)
        {
            int delta = newValue - _currentDisplayedValue;
            if (delta == 0) return;
            
            _counterTween?.Kill();
            _juiceSequence?.Kill();
            
            _crystalIcon.localScale = _baseIconScale;

            // Handle Delta Text (+10 or -40)
            _deltaText.text = delta > 0 ? $"+{delta}" : $"{delta}";
            _deltaText.color = delta > 0 ? Color.green : Color.red;
            _deltaText.gameObject.SetActive(true);
            _deltaText.rectTransform.anchoredPosition = Vector2.zero; // Reset pos
            
            int startValue = _currentDisplayedValue;
            _counterTween = DOTween.To(
                () => startValue, 
                x => {
                    _currentDisplayedValue = x;
                    _currencyText.text = x.ToString();
                }, 
                newValue, 
                0.4f
            ).SetEase(Ease.OutQuad).SetUpdate(true);

            _juiceSequence = DOTween.Sequence().SetUpdate(true);

            // 3. Chain animations together
            _juiceSequence.Join(_crystalIcon.DOPunchScale(Vector3.one * 0.8f, 0.3f, 5, 1f));
            _juiceSequence.Join(_deltaText.rectTransform.DOAnchorPosY(35f, 0.5f).SetEase(Ease.OutCubic));
            _juiceSequence.Join(_deltaText.DOFade(0f, 0.4f).SetDelay(0.3f));
            
            _juiceSequence.OnComplete(() => 
            {
                _deltaText.gameObject.SetActive(false);
                _deltaText.color = new Color(_deltaText.color.r, _deltaText.color.g, _deltaText.color.b, 1f); 
            });
        }
    }
}