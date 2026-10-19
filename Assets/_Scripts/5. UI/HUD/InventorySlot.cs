using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hierarchy")]
        [Tooltip("The child RectTransform that holds the visuals. Do NOT assign the root GameObject.")]
        [SerializeField] private RectTransform _visualRoot; 
        
        public Image Icon;
        public Image GlowImage;
        public Text AmountText;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverDuration = 0.2f;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _hoverOffsetY = 10f;

        private ItemData _cachedItem;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;

        private void Awake()
        {
            if (_visualRoot != null)
            {
                _originalPosition = _visualRoot.anchoredPosition;
                _originalScale = _visualRoot.localScale;
            }
        }

        public void Setup(ItemData item, int amount)
        {
            _cachedItem = item;
            
            if (Icon != null)
            {
                Icon.sprite = item != null ? item.Icon : null;
                Icon.preserveAspect = true;
                Icon.rectTransform.localScale = Vector3.one * amount;
            }
            
            if (GlowImage != null)
            {
                GlowImage.sprite = item != null ? item.BackgroundGlow : null;
                GlowImage.enabled = false;
            }
            
            if (AmountText != null) AmountText.text = (amount > 1) ? amount.ToString() : "";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_cachedItem != null && Icon != null)
            {
                Icon.sprite = _cachedItem.Highlighted;
                if (GlowImage != null) GlowImage.enabled = true;
            }
            
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale * _scaleMultiplier, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _hoverDuration).SetUpdate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_cachedItem != null && Icon != null)
            {
                Icon.sprite = _cachedItem.Icon;
                if (GlowImage != null) GlowImage.enabled = false;
            }
            
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition, _hoverDuration).SetUpdate(true);
            }
        }
    }
}