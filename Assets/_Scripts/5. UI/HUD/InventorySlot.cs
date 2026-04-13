using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class InventorySlot : MonoBehaviour
    {
        public Image Icon;
        public Text AmountText;

        public void Setup(ItemData item, int amount)
        {
            if (Icon != null) Icon.sprite = item != null ? item.Icon : null;
            if (AmountText != null) AmountText.text = (amount > 1) ? amount.ToString() : "";
        }
    }

}