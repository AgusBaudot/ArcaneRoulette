using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hover Animation Settings")]
        [Tooltip("How long the hover animation takes in seconds.")]
        [SerializeField] private float _animationDuration = 0.2f;
        
        [Tooltip("How much larger the UI element gets (1.1 = 10% larger).")]
        [SerializeField] private float _scaleMultiplier = 1.1f;
        
        [Tooltip("How far up the element moves in Canvas units.")]
        [SerializeField] private float _hoverOffsetY = 10f;
        
        [Tooltip("The easing curve for the animation.")]
        [SerializeField] private Ease _easeType = Ease.OutQuad;

        private RectTransform _rectTransform;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.anchoredPosition;
            _originalScale = _rectTransform.localScale;
        }

        private void OnDisable()
        {
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.anchoredPosition = _originalPosition;
                _rectTransform.localScale = _originalScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _rectTransform.DOKill();
            
            _rectTransform.DOScale(_originalScale * _scaleMultiplier, _animationDuration)
                .SetEase(_easeType).SetUpdate(true);
            _rectTransform.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _animationDuration)
                .SetEase(_easeType).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _rectTransform.DOKill();
            
            _rectTransform.DOScale(_originalScale, _animationDuration)
                .SetEase(_easeType).SetUpdate(true);
            _rectTransform.DOAnchorPos(_originalPosition, _animationDuration)
                .SetEase(_easeType).SetUpdate(true);
        }
    }
}