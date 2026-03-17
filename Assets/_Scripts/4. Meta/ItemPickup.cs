using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int amount = 1;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(item, amount);
            Destroy(gameObject);
        }
    }
}
