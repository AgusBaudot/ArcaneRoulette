using System;
using Foundation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ShopItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Hierarchy")]
        [Tooltip("The child RectTransform containing all visuals. Animating this prevents LayoutGroup fighting.")]
        [SerializeField] private RectTransform _visualRoot;

        [Header("UI Elements")]
        [SerializeField] private Image _icon;
        [SerializeField] private Image _glowImage;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private GameObject _soldOutOverlay;

        [Header("Audio")]
        [SerializeField] private AudioEventSO _failSound;
        [SerializeField] private AudioEventSO _successSound;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverDuration = 0.2f;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _hoverOffsetY = 10f;

        private Vector2 _originalPosition;
        private Vector3 _originalScale;

        private ShopUI _parent;
        private int _index;
        private int _cost;
        private bool _isRune;
        private bool _isSold;

        private RuneDefinitionSO _cachedRune;
        
        private Action<RuneDefinitionSO> _onHovered;
        private Action _onUnhovered;

        private void Awake()
        {
            _originalPosition = _visualRoot.anchoredPosition;
            _originalScale = _visualRoot.localScale;
        }

        private void OnDisable()
        {
            _visualRoot.DOKill();
            _visualRoot.anchoredPosition = _originalPosition;
            _visualRoot.localScale = _originalScale;
        }

        public void SetupRune(RuneDefinitionSO rune, bool isSold, int cost, int index, ShopUI parent, Action<RuneDefinitionSO> onHovered, Action onUnhovered)
        {
            _cachedRune = rune;
            _isRune = true;
            _onHovered = onHovered;
            _onUnhovered = onUnhovered;

            if (_icon != null && rune != null)
            {
                _icon.sprite = rune.Icon;
                _icon.preserveAspect = true;
                _icon.rectTransform.localScale = Vector3.one * rune.UIIconScale;
            }
            
            if (_glowImage != null)
            {
                _glowImage.sprite = rune != null ? rune.BackgroundGlow : null;
                _glowImage.enabled = false;
            }

            InternalSetup(isSold, cost, index, parent);
        }

        private void InternalSetup(bool isSold, int cost, int index, ShopUI parent)
        {
            _isSold = isSold;
            _cost = cost;
            _index = index;
            _parent = parent;

            _priceText.text = cost.ToString();

            if (isSold) MarkSold(false); 
            else _soldOutOverlay.SetActive(false);
        }

        public void MarkSold(bool animate = true)
        {
            _isSold = true;
            _soldOutOverlay.SetActive(true);
            _priceText.rectTransform.parent.gameObject.SetActive(false);
            _glowImage.enabled = false;

            _visualRoot.DOKill();
            _visualRoot.DOScale(_originalScale, 0.1f).SetUpdate(true);
            _onUnhovered?.Invoke();

            RectTransform stampRect = _soldOutOverlay.GetComponent<RectTransform>();
            if (!_soldOutOverlay.TryGetComponent<CanvasGroup>(out var stampGroup))
            {
                stampGroup = _soldOutOverlay.AddComponent<CanvasGroup>();
            }

            stampRect.DOKill();
            stampGroup.DOKill();

            if (animate)
            {
                stampGroup.alpha = 0f;
                stampRect.localScale = Vector3.one * 1.2f;

                stampGroup.DOFade(1f, 0.15f).SetUpdate(true);
                stampRect.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);

                EventBus.Publish(new AudioPlayRequest { Event = _successSound });
            }
            else
            {
                stampGroup.alpha = 1f;
                stampRect.localScale = Vector3.one;
            }
        }

        public void RejectPurchase()
        {
            _visualRoot.DOShakePosition(0.3f, 10f, 20, 90f, false, true).SetUpdate(true);
            EventBus.Publish(new AudioPlayRequest { Event = _failSound });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isSold) return;

            if (_isRune && _cachedRune != null && _icon != null) 
            {
                _icon.sprite = _cachedRune.Highlighted;
                _glowImage.enabled = true;
            }

            _visualRoot.DOKill();
            _visualRoot.DOScale(_originalScale * _scaleMultiplier, _hoverDuration).SetUpdate(true);
            _visualRoot.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _hoverDuration).SetUpdate(true);

            if (GameStateManager.RunState.Currency < _cost)
            {
                _priceText.color = Color.red;
            }

            if (_isRune) _onHovered?.Invoke(_cachedRune);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isSold) return;

            if (_isRune && _cachedRune != null && _icon != null) 
            {
                _icon.sprite = _cachedRune.Icon;
                _glowImage.enabled = false;
            }

            _visualRoot.DOKill();
            _visualRoot.DOScale(_originalScale, _hoverDuration).SetUpdate(true);
            _visualRoot.DOAnchorPos(_originalPosition, _hoverDuration).SetUpdate(true);
            
            _priceText.color = Color.white;

            _onUnhovered?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isSold || eventData.button != PointerEventData.InputButton.Left) return;

            _parent.AttemptPurchase(_index, _cost, _isRune, this);
        }
    }
}