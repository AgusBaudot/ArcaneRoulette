using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class ShopNPC : MonoBehaviour
    {
        [Header("Config")] [SerializeField] private PickupDropPool _runePool;

        // [SerializeField] private ArtifactDropPool _artifactPool; // Uncomment once artifact pool is done.
        [SerializeField] private GameObject _interactPrompt;

        private bool _playerInside;

        // Stock state lives on the NPC, so closing and reopening preserves it
        public List<RuneDefinitionSO> StockRunes { get; private set; } = new List<RuneDefinitionSO>();

        // public List<ArtifactDefinitionSO> StockArtifacts { get; private set; } = new List<ArtifactDefinitionSO>(); // Uncomment once artifacts are implemented 
        public List<bool> RunePurchasedState { get; private set; } = new List<bool>();
        public List<bool> ArtifactPurchasedState { get; private set; } = new List<bool>();

        private void Awake()
        {
            GenerateStock();
            if (_interactPrompt) _interactPrompt.SetActive(false);
        }

        private void OnEnable()
        {
            Helpers.Input.OnInteractPressed += HandleInteraction;
        }

        private void OnDisable()
        {
            if (Helpers.Input != null)
                Helpers.Input.OnInteractPressed -= HandleInteraction;
        }

        public void GenerateStock(bool isReroll = false)
        {
            if (!isReroll)
            {
                // Initial Generation: Wipe everything and roll 4 fresh runes
                StockRunes.Clear();
                RunePurchasedState.Clear();

                var newRunes = _runePool.GetRandomRunes(4);
                foreach (var rune in newRunes)
                {
                    StockRunes.Add(rune);
                    RunePurchasedState.Add(false); // Default to not purchased
                }
            }
            else
            {
                // Reroll Generation: Only replace slots that are not sold
                int neededRunes = 0;
                for (int i = 0; i < RunePurchasedState.Count; i++)
                {
                    if (!RunePurchasedState[i]) neededRunes++;
                }

                if (neededRunes > 0)
                {
                    // Pull exactly the amount of unpurchased slots to avoid duplicates
                    var newRunes = _runePool.GetRandomRunes(neededRunes);
                    int rollIndex = 0;

                    for (int i = 0; i < RunePurchasedState.Count; i++)
                    {
                        if (!RunePurchasedState[i])
                        {
                            // Safety check in case the pool has fewer total runes than we requested
                            if (rollIndex < newRunes.Length)
                            {
                                StockRunes[i] = newRunes[rollIndex];
                                rollIndex++;
                            }
                        }
                    }
                }
            }
        }

        private void HandleInteraction()
        {
            if (!_playerInside) return;

            EventBus.Publish(new ShopOpenRequestEvent { ShopInstance = this });
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInside = true;
                if (_interactPrompt) _interactPrompt.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInside = false;
                if (_interactPrompt) _interactPrompt.SetActive(false);
            }
        }
    }
}