using System.Collections.Generic;
using Core;
using DG.Tweening;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    public sealed class ShopNPC : MonoBehaviour, IShop
    {
        [Header("Config")] [SerializeField] private PickupDropPool _runePool;

        // [SerializeField] private ArtifactDropPool _artifactPool; // Uncomment once artifact pool is done.
        [SerializeField] private GameObject _interactPrompt;
        [SerializeField] private float _iconFadeDuration;

        private bool _playerInside;

        // Stock state lives on the NPC, so closing and reopening preserves it
        public IReadOnlyList<RuneDefinitionSO> StockRunes => _stockRunes;

        // public List<ArtifactDefinitionSO> StockArtifacts { get; private set; } = new List<ArtifactDefinitionSO>(); // Uncomment once artifacts are implemented 
        public IReadOnlyList<bool> RunePurchasedState => _runePurchasedState;
        public List<bool> ArtifactPurchasedState { get; private set; } = new List<bool>();
        
        private List<RuneDefinitionSO> _stockRunes = new();
        private List<bool> _runePurchasedState = new();

        private Image _iconSprite;
        private Tween _iconTween;

        private void Awake()
        {
            GenerateStock();
            if (_interactPrompt)
                _interactPrompt.SetActive(false);
            
            if (_interactPrompt != null)
            {
                _iconSprite = _interactPrompt.GetComponentInChildren<Image>();
                _interactPrompt.SetActive(false);
                
                if (_iconSprite != null)
                {
                    var color = _iconSprite.color;
                    color.a = 0f;
                    _iconSprite.color = color;
                }
            }
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
                _stockRunes.Clear();
                _runePurchasedState.Clear();

                var newRunes = _runePool.GetRandomRunes(5);
                foreach (var rune in newRunes)
                {
                    _stockRunes.Add(rune);
                    _runePurchasedState.Add(false); // Default to not purchased
                }
            }
            else
            {
                // Reroll Generation: Only replace slots that are not sold
                int neededRunes = 0;
                for (int i = 0; i < _runePurchasedState.Count; i++)
                {
                    if (!_runePurchasedState[i]) neededRunes++;
                }

                if (neededRunes > 0)
                {
                    // Pull exactly the amount of unpurchased slots to avoid duplicates
                    var newRunes = _runePool.GetRandomRunes(neededRunes);
                    int rollIndex = 0;

                    for (int i = 0; i < _runePurchasedState.Count; i++)
                    {
                        if (!_runePurchasedState[i])
                        {
                            // Safety check in case the pool has fewer total runes than we requested
                            if (rollIndex < newRunes.Length)
                            {
                                _stockRunes[i] = newRunes[rollIndex];
                                rollIndex++;
                            }
                        }
                    }
                }
            }
        }

        public void MarkRunePurchased(int index)
        {
            if (index >= 0 && index < _runePurchasedState.Count)
            {
                _runePurchasedState[index] = true;
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
                ShowIcon();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInside = false;
                HideIcon();
            }
        }
        
        private void ShowIcon()
        {
            if (_interactPrompt == null)
                return;
            
            _interactPrompt.SetActive(true);
            _iconTween?.Kill();
            _iconTween = _iconSprite.DOFade(1f, _iconFadeDuration);
        }

        private void HideIcon()
        {
            if (_interactPrompt == null)
                return;

            _iconTween?.Kill();
            _iconTween = _iconSprite.DOFade(0f, _iconFadeDuration)
                .OnComplete(() => _interactPrompt.SetActive(false));
        }
    }
}