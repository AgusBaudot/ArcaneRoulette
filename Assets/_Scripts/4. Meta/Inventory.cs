using System.Collections.Generic;
using UnityEngine;
using System;

namespace UI
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }
        public int Capacity = 20;

        public event Action OnInventoryChanged;

        [Serializable]
        public class InventoryItem
        {
            public ItemData Item;
            public int Amount;

            public InventoryItem(ItemData i, int a)
            {
                Item = i;
                Amount = a;
            }
        }

        public List<InventoryItem> items = new();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item == null) return false;

            if (item.Stackable)
            {
                var existing = items.Find(x => x.Item == item);
                if (existing != null)
                {
                    existing.Amount = Mathf.Min(existing.Amount + amount, item.MaxStack);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                else
                {
                    if (items.Count >= Capacity) return false;
                    items.Add(new InventoryItem(item, Mathf.Min(amount, item.MaxStack)));
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    if (items.Count >= Capacity) return false;
                    items.Add(new InventoryItem(item, 1));
                }

                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        public List<InventoryItem> GetItems()
        {
            return items;
        }
    }

}