using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;
    public Text amountText;

    public void Setup(ItemData item, int amount)
    {
        if (icon != null) icon.sprite = item != null ? item.icon : null;
        if (amountText != null) amountText.text = (amount > 1) ? amount.ToString() : "";
    }
}
