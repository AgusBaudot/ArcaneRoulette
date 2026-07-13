using Foundation;
using UnityEngine;

namespace UI
{
    public sealed class ShopUI : BaseUIPanel
    {
        [Header("References")]
        [SerializeField] private ShopItemSlotUI[] _runeSlots;
        [SerializeField] private ShopItemSlotUI[] _artifactSlots;

        [Header("Audio")]
        [SerializeField] private AudioEventSO _rerollSound;

        private IShop _currentShop;

        // ── Initialization ────────────────────────────────────────────────────

        /// <summary>
        /// Called by the UIManager to inject the specific shop's data 
        /// right before calling Show().
        /// </summary>
        public void Bind(IShop shopInstance)
        {
            _currentShop = shopInstance;
            RefreshDisplay();
        }

        // ── Show / Hide Overrides ─────────────────────────────────────────────

        public override void Hide()
        {
            base.Hide();
            TooltipSystem.Instance?.Hide();
        }

        // ── Display Logic ─────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            for (int i = 0; i < 4; i++)
            {
                if (i < _currentShop.StockRunes.Count)
                {
                    _runeSlots[i].gameObject.SetActive(true);
                    _runeSlots[i].SetupRune(_currentShop.StockRunes[i], _currentShop.RunePurchasedState[i], 40, i,
                        this);
                }
                else
                {
                    _runeSlots[i].gameObject.SetActive(false);
                }
            }

            // Uncomment once artifacts are implemented
            // for (int i = 0; i < 3; i++)
            // {
            //     int price = GetArtifactPrice(_currentShop.StockArtifacts[i]);
            //     _artifactSlots[i].SetupArtifact(_currentShop.StockArtifacts[i], _currentShop.ArtifactPurchasedState[i], price, i, this);
            // }
            // Finish uncomment once artifacts are implemented
        }
        
        // ── Interaction ───────────────────────────────────────────────────────
        
        public void AttemptPurchase(int index, int cost, bool isRune, ShopItemSlotUI slotUI)
        {
            if (GameStateManager.RunState.TrySpend(cost))
            {
                if (isRune)
                {
                    GameStateManager.RunState.AddRune(_currentShop.StockRunes[index]);
                    _currentShop.MarkRunePurchased(index);
                }
                else
                {
                    // Uncomment once artifacts are implemented
                    // GameStateManager.RunState.AddArtifact(_currentShop.StockArtifacts[index]);
                    // _currentShop.ArtifactPurchasedState[index] = true;
                    // Finish uncomment once artifacts are implemented
                }

                slotUI.MarkSold();
                TooltipSystem.Instance?.Hide();
            }
            else
            {
                slotUI.RejectPurchase();
            }
        }

        public void RerollShop()
        {
            if (GameStateManager.RunState.TrySpend(5))
            {
                EventBus.Publish(new AudioPlayRequest { Event = _rerollSound });

                _currentShop.GenerateStock(true);
                RefreshDisplay();
            }
        }
        
        // ── Artifact Helpers ──────────────────────────────────────────────────

        // Uncomment once artifacts are implemented
        // private int GetArtifactPrice(ArtifactDefinitionSO artifact)
        // {
        //     return artifact.Rarity switch
        //     {
        //         ArtifactRarity.Common => 80,
        //         ArtifactRarity.Rare => 140,
        //         ArtifactRarity.Mythic => 200,
        //         _ => 80
        //     };
        // }
        // Finish uncomment once artifacts are implemented
    }
}