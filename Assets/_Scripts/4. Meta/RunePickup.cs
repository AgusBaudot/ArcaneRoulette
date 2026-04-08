using UnityEngine;
using Foundation;
using Core;

namespace UI
{
    [RequireComponent(typeof(Collider))]

    public class RunePickup : MonoBehaviour
    {
        public RuneDefinitionSO runeDefinition;
        public int amount = 1;
        public bool destroyOnPick = true;

        private VolatileRunState RunState => GameStateManager.RunState;

        private void Awake()
        {
            if (GetComponent<Collider>() is Collider col)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Assuming player has tag "Player". Adjust if needed.
            if (!other.CompareTag("Player"))
                return;

            if (runeDefinition == null)
            {
                Debug.LogWarning("RunePickup: No RuneDefinitionSO assigned.");
                return;
            }

            RunState.AddRune(runeDefinition, amount);
            Debug.Log(
                $"RunePickup: picked up {amount}x {runeDefinition.name} -> now available {RunState.AvailableCount(runeDefinition)}");

            // Update crafting UI inventory display if exists
            var craftingUI = FindObjectOfType<SpellCraftingUI>();
            // if (craftingUI != null)
            //     craftingUI.RefreshInventoryDisplay();

            if (destroyOnPick)
                Destroy(gameObject);
        }
    }

}