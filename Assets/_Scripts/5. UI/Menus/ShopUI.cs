using DG.Tweening;
using Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class ShopUI : BaseUIPanel
    {
        [Header("References")]
        [SerializeField] private ShopItemSlotUI[] _runeSlots;
        [SerializeField] private ShopItemSlotUI[] _artifactSlots;
        [SerializeField] private Button _backButton;

        [Header("Audio")]
        [SerializeField] private AudioEventSO _rerollSound;
        [SerializeField] private AudioEventSO _buySound;
        
        [Header("Local Tooltip References")]
        [SerializeField] private GameObject _tooltipContainer;
        [SerializeField] private CanvasGroup _tooltipCanvasGroup;
        [SerializeField] private float _tooltipFadeDuration;
        [SerializeField] private TextMeshProUGUI _tooltipNameText;
        [SerializeField] private TextMeshProUGUI _tooltipTypeText;
        [SerializeField] private TextMeshProUGUI _tooltipDescriptionText;

        private IShop _currentShop;

        // ── Initialization ────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _backButton.onClick.AddListener(RequestClose);
            
            if (_tooltipCanvasGroup != null) _tooltipCanvasGroup.alpha = 0f;
            if (_tooltipContainer != null) _tooltipContainer.SetActive(false);
        }
        
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

        public override void Show()
        {
            if (TooltipSystem.Instance != null)
                TooltipSystem.Instance.SetEnabled(false);

            ClearLocalTooltipInstantly();
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
            
            if (TooltipSystem.Instance != null)
                TooltipSystem.Instance.SetEnabled(true);
                
            ClearLocalTooltipInstantly();
        }

        // ── Display Logic ─────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            for (int i = 0; i < _runeSlots.Length; i++)
            {
                if (i < _currentShop.StockRunes.Count)
                {
                    _runeSlots[i].gameObject.SetActive(true);
                    _runeSlots[i].SetupRune(_currentShop.StockRunes[i], _currentShop.RunePurchasedState[i], 40, i,
                        this, ShowLocalTooltip, ClearLocalTooltip);
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
        
        private void ShowLocalTooltip(RuneDefinitionSO rune)
        {
            if (rune == null) return;

            if (_tooltipNameText != null) _tooltipNameText.text = rune.Name;
            if (_tooltipTypeText != null) _tooltipTypeText.text = rune.Type;
            if (_tooltipDescriptionText != null) _tooltipDescriptionText.text = rune.Description;
            
            if (_tooltipContainer != null) _tooltipContainer.SetActive(true);
            
            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.DOKill();
                _tooltipCanvasGroup.DOFade(1f, _tooltipFadeDuration).SetUpdate(true);
            }
        }

        private void ClearLocalTooltip()
        {
            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.DOKill();
                _tooltipCanvasGroup.DOFade(0f, _tooltipFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() => 
                    {
                        if (_tooltipContainer != null) _tooltipContainer.SetActive(false);
                    });
            }
            else
            {
                ClearLocalTooltipInstantly();
            }
        }
        
        private void ClearLocalTooltipInstantly()
        {
            if (_tooltipContainer != null) _tooltipContainer.SetActive(false);
            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.DOKill();
                _tooltipCanvasGroup.alpha = 0f;
            }
        }
        
        // ── Interaction ───────────────────────────────────────────────────────
        
        public void AttemptPurchase(int index, int cost, bool isRune, ShopItemSlotUI slotUI)
        {
            if (GameStateManager.RunState.TrySpend(cost))
            {
                if (_buySound != null)
                {
                    EventBus.Publish(new AudioPlayRequest { Event = _buySound });
                }
                
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