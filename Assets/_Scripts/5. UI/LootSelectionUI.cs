using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Foundation;

namespace UI
{
    /// <summary>
    /// Manages the "Boon" style selection screen. 
    /// Displays a pool of runes and handles the player's selection.
    /// </summary>
    public class LootSelectionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private GameObject _lootOptionPrefab;

        // Keep track of instantiated options to clear them when the window closes
        private readonly List<GameObject> _spawnedOptions = new();

        /// <summary>
        /// Populates the UI with a pool of runes and displays the selection screen.
        /// </summary>
        /// <param name="runePool">The runes available to choose from.</param>
        /// <param name="onOptionSelected">Callback executed when the player makes a choice.</param>
        public void ShowOptions(List<RuneDefinitionSO> runePool, Action<RuneDefinitionSO> onOptionSelected)
        {
            ClearOptions();
            gameObject.SetActive(true);

            foreach (var rune in runePool)
            {
                GameObject optionGo = Instantiate(_lootOptionPrefab, _optionsContainer);
                _spawnedOptions.Add(optionGo);

                // Ideally, you'll have a specific component for the Boon panel (e.g. LootOptionUI)
                // If you are temporarily reusing RuneTileUI, it would look like this:
                if (optionGo.TryGetComponent<LootOptionUI>(out var lootOption))
                {
                    lootOption.Init(rune, () =>
                    {
                        HandleSelection(rune, onOptionSelected);
                    });
                }
            }
        }

        /// <summary>
        /// Handles the click event from an individual loot option.
        /// </summary>
        private void HandleSelection(RuneDefinitionSO selectedRune, Action<RuneDefinitionSO> callback)
        {
            // 1. Notify the system (Inventory/Attunement) about the chosen rune
            callback?.Invoke(selectedRune);

            // 2. Clean up and hide the UI
            Close();
        }

        public void Close()
        {
            ClearOptions();
            gameObject.SetActive(false);
        }

        private void ClearOptions()
        {
            foreach (var option in _spawnedOptions)
            {
                if (option != null)
                {
                    Destroy(option);
                }
            }
            _spawnedOptions.Clear();
        }
    }
}