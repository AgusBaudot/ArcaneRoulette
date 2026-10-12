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
        [Tooltip("The CanvasGroup on the sprite object to fade when hovered or selected.")]
        [SerializeField] private CanvasGroup _selectedSpriteGroup;
        
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
        private Tween _alphaTween;

        private void Awake()
        {
            _layoutElement = GetComponent<LayoutElement>();
            
            if (_selectedSpriteGroup != null)
            {
                _selectedSpriteGroup.alpha = 0f;
                _selectedSpriteGroup.gameObject.SetActive(false);
            }

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
            _alphaTween?.Kill();
            
            _layoutElement.preferredWidth = _baseSize.x;
            _layoutElement.preferredHeight = _baseSize.y;
            
            if (_visualRoot != null)
            {
                _visualRoot.anchoredPosition = _baseVisualPos;
                _visualRoot.localScale = _baseVisualScale;
            }
            
            if (_selectedSpriteGroup != null)
            {
                _selectedSpriteGroup.alpha = 0f;
                _selectedSpriteGroup.gameObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => Focus();
        public void OnPointerExit(PointerEventData eventData) => Unfocus();
        public void OnSelect(BaseEventData eventData) => Focus();
        public void OnDeselect(BaseEventData eventData) => Unfocus();

        private void Focus()
        {
            AnimateSpriteAlpha(1f);
            AnimateTo(_baseSize * _hoverSizeMultiplier, _baseVisualPos + new Vector2(_hoverMoveX, 0f), _baseVisualScale * _hoverSizeMultiplier);
        }

        private void Unfocus()
        {
            AnimateSpriteAlpha(0f);
            AnimateTo(_baseSize, _baseVisualPos, _baseVisualScale);
        }

        private void AnimateSpriteAlpha(float targetAlpha)
        {
            if (_selectedSpriteGroup == null) return;

            _alphaTween?.Kill();

            if (targetAlpha > 0f && !_selectedSpriteGroup.gameObject.activeSelf)
            {
                _selectedSpriteGroup.gameObject.SetActive(true);
            }

            _alphaTween = _selectedSpriteGroup.DOFade(targetAlpha, _tweenDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => 
                {
                    if (targetAlpha == 0f)
                    {
                        _selectedSpriteGroup.gameObject.SetActive(false);
                    }
                });
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
    }
}