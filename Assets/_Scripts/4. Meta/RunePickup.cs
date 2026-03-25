using UnityEngine;
using Foundation;
using Core;

[RequireComponent(typeof(Collider2D))]
public class RunePickup : MonoBehaviour
{
    public RuneDefinitionSO runeDefinition;
    public int amount = 1;
    public bool destroyOnPick = true;

    private VolatileRunState RunState => GameStateManager.RunState;

    private void Awake()
    {
        if (GetComponent<Collider2D>() is Collider2D col)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
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
        Debug.Log($"RunePickup: picked up {amount}x {runeDefinition.name} -> now available {RunState.AvailableCount(runeDefinition)}");

        // Update crafting UI inventory display if exists
        var craftingUI = FindObjectOfType<SpellCraftingUI>();
        if (craftingUI != null)
            craftingUI.RefreshInventoryDisplay();

        if (destroyOnPick)
            Destroy(gameObject);
    }
}
