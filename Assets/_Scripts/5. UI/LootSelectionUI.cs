using System.Collections.Generic;
using Foundation;
using World;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class LootSelectionUI : BaseUIPanel
    {
        [Header("Rune Display")]
        [SerializeField] private Transform _runeContainer;
        [SerializeField] private LootOptionUI _lootOptionPrefab;

        [Header("Confirm")]
        [SerializeField] private Button _confirmButton;

        [Header("Settings — designer-tunable")]
        [Tooltip("How many runes to show (n).")]
        [SerializeField] private int _runesToShow = 3;

        [Tooltip("How many the player may keep (m). Clamped to n at runtime.")]
        [SerializeField] private int _runesToSelect = 1;

        [Header("Drop Pool")]
        [SerializeField] private PickupDropPool _dropPool;

        // ── Runtime ──────────────────────────────────────────────────────────

        private readonly List<LootOptionUI> _optionPool = new();
        private readonly List<int> _selectionOrder = new();
        private int _effectiveMax;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            base.Awake();
            
            _confirmButton.onClick.AddListener(OnConfirm);

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
            if (_dropPool == null)
            {
                Debug.LogWarning("[LootSelectionUI] No PickupDropPool assigned.");
                return;
            }
            
            _selectionOrder.Clear();

            RuneDefinitionSO[] runes = _dropPool.GetRandomRunes(_runesToShow);
            _effectiveMax = Mathf.Min(_runesToSelect, runes.Length);

            for (int i = 0; i < _optionPool.Count; i++)
            {
                if (i < runes.Length)
                {
                    _optionPool[i].Init(runes[i], null);
                    _optionPool[i].SetSelected(false);
                    _optionPool[i].gameObject.SetActive(true);
                }
                else
                {
                    _optionPool[i].SetSelected(false);
                }
            }
            
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
            
            _selectionOrder.Clear();
        }
        
        // private void Show()
        // {
        //     if (_dropPool == null)
        //     {
        //         Debug.LogWarning("[LootSelectionUI] No PickupDropPool assigned.");
        //         return;
        //     }
        //
        //     _isShowing = true;
        //     _selectionOrder.Clear();
        //
        //     RuneDefinitionSO[] runes = _dropPool.GetRandomRunes(_runesToShow);
        //     _effectiveMax = Mathf.Min(_runesToSelect, runes.Length);
        //
        //     // Clear any previously spawned option tiles.
        //     foreach (Transform child in _runeContainer)
        //         Destroy(child.gameObject);
        //
        //     _options = new LootOptionUI[runes.Length];
        //
        //     for (int i = 0; i < runes.Length; i++)
        //     {
        //         int captured = i;
        //         LootOptionUI option = Instantiate(_lootOptionPrefab, _runeContainer);
        //         option.Init(runes[i], () => OnOptionClicked(captured));
        //         _options[i] = option;
        //     }
        //
        //     _panel.SetActive(true);
        //     Time.timeScale = 0f;
        //     Helpers.Input.EnableUIInput();
        // }
        //
        // private void Hide()
        // {
        //     _panel.SetActive(false);
        //     _selectionOrder.Clear();
        //     _isShowing = false;
        //     Time.timeScale = 1f;
        //     Helpers.Input.EnablePlayerInput();
        // }

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
            // 0 selected is a valid no-op — loop simply doesn't execute.
            foreach (int index in _selectionOrder)
                GameStateManager.RunState.AddRune(_optionPool[index].Rune);

            RequestClose();
        }
    }
}