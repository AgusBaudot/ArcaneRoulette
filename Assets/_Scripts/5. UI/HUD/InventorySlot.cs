using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Icon;
        public Text AmountText;

        private ItemData _cachedItem;

        public void Setup(ItemData item, int amount)
        {
            _cachedItem = item;
            
            if (Icon != null)
            {
                Icon.sprite = item != null ? item.Icon : null;
                Icon.preserveAspect = true;
                Icon.rectTransform.localScale = Vector3.one * amount;
            }
            if (AmountText != null) AmountText.text = (amount > 1) ? amount.ToString() : "";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_cachedItem != null && Icon != null)
            {
                Icon.sprite = _cachedItem.Highlighted;
            }
            
            transform.DOKill();
            transform.DOScale(1.15f, 0.2f).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_cachedItem != null && Icon != null)
            {
                Icon.sprite = _cachedItem.Icon;
            }
            
            transform.DOKill();
            transform.DOScale(1f, 0.2f).SetUpdate(true);
        }
    }

}