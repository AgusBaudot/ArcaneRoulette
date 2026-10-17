using System.Collections.Generic;
using DG.Tweening;
using Foundation;
using TMPro;
using World;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class LootSelectionUI : BaseUIPanel
    {
        [Header("UI Animation References")] 
        [SerializeField] private RectTransform _runeContainer;
        [SerializeField] private RectTransform _tooltip;
        [SerializeField] private RectTransform _confirm;

        [Header("Animation Settings")] 
        [Tooltip("How high up (in Canvas units) the elements start.")] 
        [SerializeField] private float _dropOffsetY = 1000f;
        [Tooltip("How long it takes for a single element to fall.")]
        [SerializeField] private float _dropDuration = 0.25f;
        [Tooltip("The strength of the Z-axis rotation stumble (degrees).")] 
        [SerializeField] private float _shakeStrength = 5f;
        [Tooltip("How long the stumble wobbles.")] 
        [SerializeField] private float _shakeDuration = 0.2f;

        [Header("Rune Display")] 
        [SerializeField] private LootOptionUI _lootOptionPrefab;

        [Header("Confirm")] 
        [SerializeField] private Button _confirmButton;

        [Header("Settings")]
        [Tooltip("How many runes to show (n).")] 
        [SerializeField] private int _runesToShow = 3;
        [Tooltip("How many the player may keep (m). Clamped to n at runtime.")]
        [SerializeField] private int _runesToSelect = 1;

        [Header("Drop Pool")]
        [SerializeField] private PickupDropPool _dropPool;
        
        [Header("Local Tooltip References")]
        [SerializeField] private TextMeshProUGUI _tooltipNameText;
        [SerializeField] private TextMeshProUGUI _tooltipTypeText;
        [SerializeField] private TextMeshProUGUI _tooltipDescriptionText;
        [SerializeField] private GameObject _tooltipContainer;

        // ── Runtime ──────────────────────────────────────────────────────────

        private readonly List<LootOptionUI> _optionPool = new();
        private readonly List<int> _selectionOrder = new();
        private int _effectiveMax;

        private Vector2 _runeOriginalPos;
        private Vector2 _tooltipOriginalPos;
        private Vector2 _confirmOriginalPos;
        private Sequence _showSequence;

        // ── Unity ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _confirmButton.onClick.AddListener(OnConfirm);

            if (_runeContainer != null) _runeOriginalPos = _runeContainer.anchoredPosition;
            if (_tooltip != null) _tooltipOriginalPos = _tooltip.anchoredPosition;
            if (_confirm != null) _confirmOriginalPos = _confirm.anchoredPosition;

            for (int i = 0; i < _runesToShow; i++)
            {
                int capturedIndex = i;

                LootOptionUI option = Instantiate(_lootOptionPrefab, _runeContainer);

                option.Init(null, () => OnOptionClicked(capturedIndex));
                option.gameObject.SetActive(false);

                _optionPool.Add(option);
            }
        }

        // ── Show / Hide ───────────────────────────────────────────────────────

        public override void Show()
        {
            if (_dropPool == null) return;
    
            // 1. Turn off the global hovering tooltip
            if (TooltipSystem.Instance != null)
                TooltipSystem.Instance.SetEnabled(false);

            // 2. Hide local tooltip by default
            ClearLocalTooltip();
    
            _selectionOrder.Clear();
            RuneDefinitionSO[] runes = _dropPool.GetRandomRunes(_runesToShow);
            _effectiveMax = Mathf.Min(_runesToSelect, runes.Length);

            for (int i = 0; i < _optionPool.Count; i++)
            {
                if (i < runes.Length)
                {
                    int capturedIndex = i;
                    RuneDefinitionSO capturedRune = runes[i];

                    // 3. Pass hover/unhover actions into the option's Init method
                    _optionPool[i].Init(
                        capturedRune, 
                        () => OnOptionClicked(capturedIndex),
                        () => ShowLocalTooltip(capturedRune),
                        () => ClearLocalTooltip()
                    );
            
                    _optionPool[i].SetSelected(false);
                    _optionPool[i].gameObject.SetActive(true);
                }
                else
                {
                    _optionPool[i].gameObject.SetActive(false);
                }
            }
    
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
            _selectionOrder.Clear();
    
            if (TooltipSystem.Instance != null)
                TooltipSystem.Instance.SetEnabled(true);
        }

        private void ShowLocalTooltip(RuneDefinitionSO rune)
        {
            if (rune == null) return;

            if (_tooltipContainer != null) _tooltipContainer.SetActive(true);
            _tooltipNameText.text = rune.Name;
            _tooltipTypeText.text = rune.Type;
            _tooltipDescriptionText.text = rune.Description;
        }

        private void ClearLocalTooltip()
        {
            if (_tooltipContainer != null) _tooltipContainer.SetActive(false);
            _tooltipNameText.text = string.Empty;
            _tooltipTypeText.text = string.Empty;
            _tooltipDescriptionText.text = string.Empty;
        }

        // ── Selection logic ───────────────────────────────────────────────────

        private void OnOptionClicked(int index)
        {
            if (_selectionOrder.Contains(index))
            {
                _selectionOrder.Remove(index);
            }
            else
            {
                if (_selectionOrder.Count >= _effectiveMax)
                {
                    int evicted = _selectionOrder[0];
                    _selectionOrder.RemoveAt(0);
                    _optionPool[evicted].SetSelected(false);
                }

                _selectionOrder.Add(index);
            }

            _optionPool[index].SetSelected(_selectionOrder.Contains(index));
        }

        // ── Confirm ───────────────────────────────────────────────────────────

        private void OnConfirm()
        {
            foreach (int index in _selectionOrder)
                GameStateManager.RunState.AddRune(_optionPool[index].Rune);

            RequestClose();
        }
    }
}