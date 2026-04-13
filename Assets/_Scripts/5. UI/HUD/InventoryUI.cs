using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class InventoryUI : MonoBehaviour
    {
        public Transform ItemsParent;
        public GameObject SlotPrefab;

        private List<GameObject> _slots = new();

        void Start()
        {
            if (Inventory.Instance != null) Inventory.Instance.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }

        void OnDestroy()
        {
            if (Inventory.Instance != null) Inventory.Instance.OnInventoryChanged -= RefreshUI;
        }

        public void RefreshUI() //Can be made private
        {
            foreach (var s in _slots) Destroy(s);
            _slots.Clear();

            if (Inventory.Instance == null) return;

            foreach (var it in Inventory.Instance.GetItems())
            {
                var go = Instantiate(SlotPrefab, ItemsParent);
                var slot = go.GetComponent<InventorySlot>();
                if (slot != null) slot.Setup(it.Item, it.Amount);
                _slots.Add(go);
            }
        }
    }

}