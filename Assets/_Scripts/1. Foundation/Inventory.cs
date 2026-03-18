using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public int capacity = 20;

    public event Action OnInventoryChanged;

    [Serializable]
    public class InventoryItem
    {
        public ItemData item;
        public int amount;
        public InventoryItem(ItemData i, int a) { item = i; amount = a; }
    }

    public List<InventoryItem> items = new List<InventoryItem>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return false;

        if (item.stackable)
        {
            var existing = items.Find(x => x.item == item);
            if (existing != null)
            {
                existing.amount = Mathf.Min(existing.amount + amount, item.maxStack);
                OnInventoryChanged?.Invoke();
                return true;
            }
            else
            {
                if (items.Count >= capacity) return false;
                items.Add(new InventoryItem(item, Mathf.Min(amount, item.maxStack)));
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                if (items.Count >= capacity) return false;
                items.Add(new InventoryItem(item, 1));
            }
            OnInventoryChanged?.Invoke();
            return true;
        }
    }

    public List<InventoryItem> GetItems() { return items; }
}
