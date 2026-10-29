using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UI
{
    public sealed class UIHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Main Element Settings")]
        [Tooltip("The main visual element to animate. Defaults to this object's RectTransform if null.")]
        [SerializeField] private RectTransform _targetRect;
        
        [Tooltip("How long the hover animation takes in seconds.")]
        [SerializeField] private float _animationDuration = 0.2f;
        
        [Tooltip("How much larger the UI element gets (1.1 = 10% larger).")]
        [SerializeField] private float _scaleMultiplier = 1.1f;
        
        [Tooltip("How far up the element moves in Canvas units.")]
        [SerializeField] private float _hoverOffsetY = 10f;
        
        [Tooltip("The easing curve for the animation.")]
        [SerializeField] private Ease _easeType = Ease.OutQuad;

        [Header("Shadow Settings (Optional)")]
        [Tooltip("The shadow element. MUST be a sibling behind the target, NOT a child.")]
        [SerializeField] private RectTransform _shadowRect;
        
        [Tooltip("How much the shadow scales. Usually much less than the main element to simulate distance.")]
        [SerializeField] private float _shadowScaleMultiplier = 1.02f;
        
        [Tooltip("Requires a CanvasGroup on the shadow. Simulates diffusion by lowering alpha.")]
        [SerializeField] private CanvasGroup _shadowCanvasGroup;
        
        [Tooltip("The alpha value the shadow fades to when hovered.")]
        [SerializeField] private float _shadowHoverAlpha = 0.6f;

        private Vector2 _originalPosition;
        private Vector3 _originalScale;
        
        private Vector3 _originalShadowScale;
        private float _originalShadowAlpha;

        private void Awake()
        {
            if (_targetRect == null) 
                _targetRect = GetComponent<RectTransform>();

            _originalPosition = _targetRect.anchoredPosition;
            _originalScale = _targetRect.localScale;

            if (_shadowRect != null)
            {
                _originalShadowScale = _shadowRect.localScale;
            }
            
            if (_shadowCanvasGroup != null)
            {
                _originalShadowAlpha = _shadowCanvasGroup.alpha;
            }
        }

        private void OnDisable()
        {
            if (_targetRect != null)
            {
                _targetRect.DOKill();
                _targetRect.anchoredPosition = _originalPosition;
                _targetRect.localScale = _originalScale;
            }

            if (_shadowRect != null)
            {
                _shadowRect.DOKill();
                _shadowRect.localScale = _originalShadowScale;
            }

            if (_shadowCanvasGroup != null)
            {
                _shadowCanvasGroup.DOKill();
                _shadowCanvasGroup.alpha = _originalShadowAlpha;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetRect.DOKill();
            _targetRect.DOScale(_originalScale * _scaleMultiplier, _animationDuration)
                .SetEase(_easeType).SetUpdate(true);
            _targetRect.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _animationDuration)
                .SetEase(_easeType).SetUpdate(true);

            if (_shadowRect != null)
            {
                _shadowRect.DOKill();
                _shadowRect.DOScale(_originalShadowScale * _shadowScaleMultiplier, _animationDuration)
                    .SetEase(_easeType).SetUpdate(true);
            }

            if (_shadowCanvasGroup != null)
            {
                _shadowCanvasGroup.DOKill();
                _shadowCanvasGroup.DOFade(_shadowHoverAlpha, _animationDuration)
                    .SetEase(_easeType).SetUpdate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetRect.DOKill();
            _targetRect.DOScale(_originalScale, _animationDuration)
                .SetEase(_easeType).SetUpdate(true);
            _targetRect.DOAnchorPos(_originalPosition, _animationDuration)
                .SetEase(_easeType).SetUpdate(true);

            if (_shadowRect != null)
            {
                _shadowRect.DOKill();
                _shadowRect.DOScale(_originalShadowScale, _animationDuration)
                    .SetEase(_easeType).SetUpdate(true);
            }

            if (_shadowCanvasGroup != null)
            {
                _shadowCanvasGroup.DOKill();
                _shadowCanvasGroup.DOFade(_originalShadowAlpha, _animationDuration)
                    .SetEase(_easeType).SetUpdate(true);
            }
        }
    }
}