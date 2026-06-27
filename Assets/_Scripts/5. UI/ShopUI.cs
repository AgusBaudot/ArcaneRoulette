using System;
using Foundation;
using UnityEngine;
using World;

namespace UI
{
    public sealed class ShopUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private GameObject _panel;

        [SerializeField] private ShopItemSlotUI[] _runeSlots;
        [SerializeField] private ShopItemSlotUI[] _artifactSlots;

        [Header("Audio")] [SerializeField] private AudioEventSO _menuOpenSound;
        [SerializeField] private AudioEventSO _menuCloseSound;
        [SerializeField] private AudioEventSO _rerollSound;

        private ShopNPC _currentShop;

        private void Awake()
        {
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ShopOpenRequestEvent>(OnShopOpenRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ShopOpenRequestEvent>(OnShopOpenRequested);
        }

        private void OnShopOpenRequested(ShopOpenRequestEvent evt)
        {
            _currentShop = evt.ShopInstance;
            _panel.SetActive(true);

            Time.timeScale = 0f;
            Helpers.Input.EnableUIInput();

            EventBus.Publish(new AudioPlayRequest { Event = _menuOpenSound });

            RefreshDisplay();
        }

        public void Close()
        {
            _panel.SetActive(false);
            TooltipSystem.Instance?.Hide();

            // Strict Context Switching Order
            Time.timeScale = 1f;
            Helpers.Input.EnablePlayerInput();

            EventBus.Publish(new AudioPlayRequest { Event = _menuCloseSound });
        }

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

        public void AttemptPurchase(int index, int cost, bool isRune, ShopItemSlotUI slotUI)
        {
            if (GameStateManager.RunState.TrySpend(cost))
            {
                if (isRune)
                {
                    GameStateManager.RunState.AddRune(_currentShop.StockRunes[index]);
                    _currentShop.RunePurchasedState[index] = true;
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