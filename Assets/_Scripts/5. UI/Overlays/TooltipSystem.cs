using TMPro;
using UnityEngine;
using Foundation;
using UnityEngine.UI;
using DG.Tweening;

namespace UI
{
    // Singleton — lives on the Canvas. RuneTileUI calls Show/Hide.
    // Positions itself at a fixed pixel offset from the hovered tile's RectTransform.
    // Clamps to screen bounds so it never clips off-screen edges.
    public sealed class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem Instance { get; private set; }

        [Header("Panel references")]
        [SerializeField] private RectTransform _panelRect;

        [Header("Text fields")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("Layout")]
        [Tooltip("Pixel offset from the tile's pivot in screen space.")]
        [SerializeField] private Vector2 _offset = new(10f, -10f);

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.15f;

        private CanvasGroup _canvasGroup;
        private Canvas _canvas;
        private bool _enabled = true;
        
        private Tween _fadeTween;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvas = GetComponentInParent<Canvas>();

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
            
            if (Instance == this) 
                Instance = null;
        }

        public void Show(RuneDefinitionSO rune, RectTransform tileRect)
        {
            if (rune == null || !_enabled)
                return;

            _nameText.text = rune.Name;
            _typeText.text = rune.Type;
            _descriptionText.text = rune.Description;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);
            
            PositionTooltip(tileRect);

            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
        }

        public void Hide()
        {
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration).SetUpdate(true);
        }

        private void PositionTooltip(RectTransform tileRect)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, tileRect.position);
            screenPoint += _offset;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                screenPoint,
                null,
                out Vector2 localPoint);

            _panelRect.localPosition = localPoint;

            ClampToScreen();
        }

        private void ClampToScreen()
        {
            var canvasRect = _canvas.GetComponent<RectTransform>();
            Vector3 pos = _panelRect.localPosition;

            float canvasHalfW = canvasRect.rect.width * 0.5f;
            float canvasHalfH = canvasRect.rect.height * 0.5f;
            float panelHalfW = _panelRect.rect.width * 0.5f;
            float panelHalfH = _panelRect.rect.height * 0.5f;

            pos.x = Mathf.Clamp(pos.x, -canvasHalfW + panelHalfW, canvasHalfW - panelHalfW);
            pos.y = Mathf.Clamp(pos.y, -canvasHalfH + panelHalfH, canvasHalfH - panelHalfH);

            _panelRect.localPosition = pos;
        }
        
        public void ToggleEnabled()
        {
            _enabled = !_enabled;
            if (!_enabled)
                Hide();
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!_enabled)
                Hide();
        }
    }
}