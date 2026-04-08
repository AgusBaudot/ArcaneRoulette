using UnityEngine;

namespace UI
{
    public class ItemPickup : MonoBehaviour
    {
        public ItemData Item;
        public int Amount = 1;

        // Prevent double pickup when multiple trigger events fire in the same frame.
        private bool pickedUp;

        void OnTriggerEnter(Collider other)
        {
            if (pickedUp) return;
            if (!other.CompareTag("Player")) return;

            pickedUp = true;

            if (Inventory.Instance != null)
            {
                Inventory.Instance.AddItem(Item, Amount);
                Destroy(gameObject);
            }
        }
    }
}
