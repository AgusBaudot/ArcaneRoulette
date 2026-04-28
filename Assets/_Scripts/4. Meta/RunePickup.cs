using UnityEngine;
using Foundation;
using Core;
using TMPro;

namespace UI
{
    [RequireComponent(typeof(Collider))]
    public class RunePickup : MonoBehaviour
    {
        public RuneDefinitionSO RuneDefinition;
        public int Amount = 1;
        public bool DestroyOnPick = true;

        [SerializeField] private TMP_Text _pickupLabel;

        private VolatileRunState RunState => GameStateManager.RunState;

        private void Awake()
        {
            if (GetComponent<Collider>() is Collider col)
            {
                col.isTrigger = true;
            }

            if (_pickupLabel == null)
                _pickupLabel = GetComponentInChildren<TMP_Text>();

            UpdateLabelText();
        }

        private void OnValidate()
        {
            if (_pickupLabel == null)
                _pickupLabel = GetComponentInChildren<TMP_Text>();

            UpdateLabelText();
        }

        private void UpdateLabelText()
        {
            if (_pickupLabel == null)
                return;

            _pickupLabel.text = RuneDefinition != null ? RuneDefinition.name : "Unknown";
        }

        private void OnTriggerEnter(Collider other)
        {
            // Assuming player has tag "Player". Adjust if needed.
            if (!other.CompareTag("Player"))
                return;

            if (RuneDefinition == null)
            {
                Debug.LogWarning("RunePickup: No RuneDefinitionSO assigned.");
                return;
            }

            RunState.AddRune(RuneDefinition, Amount);
            Debug.Log(
                $"RunePickup: picked up {Amount}x {RuneDefinition.name} -> now available {RunState.AvailableCount(RuneDefinition)}");

            // Update crafting UI inventory display if exists
            var craftingUI = FindObjectOfType<SpellCraftingUI>();
            // if (craftingUI != null)
            //     craftingUI.RefreshInventoryDisplay();

            if (DestroyOnPick)
                Destroy(gameObject);
        }
    }

}