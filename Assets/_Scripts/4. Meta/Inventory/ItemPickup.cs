using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int amount = 1;

    // Prevent double pickup when multiple trigger events fire in the same frame.
    private bool pickedUp;

    void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (!other.CompareTag("Player")) return;

        pickedUp = true;

        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(item, amount);
            Destroy(gameObject);
        }
    }
}
