using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public Transform itemsParent;
    public GameObject slotPrefab;

    private List<GameObject> slots = new List<GameObject>();

    void Start()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnInventoryChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        foreach (var s in slots) Destroy(s);
        slots.Clear();

        if (Inventory.Instance == null) return;

        foreach (var it in Inventory.Instance.GetItems())
        {
            var go = Instantiate(slotPrefab, itemsParent);
            var slot = go.GetComponent<InventorySlot>();
            if (slot != null) slot.Setup(it.item, it.amount);
            slots.Add(go);
        }
    }
}
