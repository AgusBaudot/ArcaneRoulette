using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(Selectable), typeof(LayoutElement))]
    public sealed class UIHoverSpriteToggle : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, 
        ISelectHandler, IDeselectHandler
    {
        [Header("Visuals")]
        [Tooltip("The sprite object to enable when hovered or selected.")]
        [SerializeField] private GameObject _selectedSprite;
        
        [Tooltip("The child RectTransform containing the text and sprite to move horizontally.")]
        [SerializeField] private RectTransform _visualRoot;

        [Header("Animation")]
        [SerializeField] private float _hoverSizeMultiplier = 1.1f;
        [SerializeField] private float _hoverMoveX = 10f;
        [SerializeField] private float _tweenDuration = 0.15f;

        private LayoutElement _layoutElement;
        private Vector2 _baseSize;
        private Vector2 _baseVisualPos;
        private Vector3 _baseVisualScale;
        
        private Tween _sizeTween;
        private Tween _moveTween;
        private Tween _visualScaleTween;

        private void Awake()
        {
            _layoutElement = GetComponent<LayoutElement>();
            SetSpriteActive(false);

            _baseSize = new Vector2(_layoutElement.preferredWidth, _layoutElement.preferredHeight);
            
            if (_visualRoot != null)
            {
                _baseVisualPos = _visualRoot.anchoredPosition;
                _baseVisualScale = _visualRoot.localScale;
            }
        }

        private void OnDisable()
        {
            _sizeTween?.Kill();
            _moveTween?.Kill();
            _visualScaleTween?.Kill();
            
            _layoutElement.preferredWidth = _baseSize.x;
            _layoutElement.preferredHeight = _baseSize.y;
            
            if (_visualRoot != null)
            {
                _visualRoot.anchoredPosition = _baseVisualPos;
                _visualRoot.localScale = _baseVisualScale;
            }
            
            SetSpriteActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData) => Focus();
        public void OnPointerExit(PointerEventData eventData) => Unfocus();
        public void OnSelect(BaseEventData eventData) => Focus();
        public void OnDeselect(BaseEventData eventData) => Unfocus();

        private void Focus()
        {
            SetSpriteActive(true);
            AnimateTo(_baseSize * _hoverSizeMultiplier, _baseVisualPos + new Vector2(_hoverMoveX, 0f), _baseVisualScale * _hoverSizeMultiplier);
        }

        private void Unfocus()
        {
            SetSpriteActive(false);
            AnimateTo(_baseSize, _baseVisualPos, _baseVisualScale);
        }

        private void AnimateTo(Vector2 targetSize, Vector2 targetVisualPos, Vector3 targetVisualScale)
        {
            _sizeTween?.Kill();
            _moveTween?.Kill();
            _visualScaleTween?.Kill();

            _sizeTween = DOTween.To(
                () => new Vector2(_layoutElement.preferredWidth, _layoutElement.preferredHeight),
                x => 
                {
                    _layoutElement.preferredWidth = x.x;
                    _layoutElement.preferredHeight = x.y;
                },
                targetSize, 
                _tweenDuration
            ).SetEase(Ease.OutQuad);

            if (_visualRoot != null)
            {
                _moveTween = _visualRoot.DOAnchorPos(targetVisualPos, _tweenDuration).SetEase(Ease.OutQuad);
                _visualScaleTween = _visualRoot.DOScale(targetVisualScale, _tweenDuration).SetEase(Ease.OutQuad);
            }
        }

        private void SetSpriteActive(bool isActive)
        {
            if (_selectedSprite != null && _selectedSprite.activeSelf != isActive)
            {
                _selectedSprite.SetActive(isActive);
            }
        }
    }
}