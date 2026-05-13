using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class LootSelectionUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

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
        [SerializeField] private World.PickupDropPool _dropPool;

        // ── Runtime ──────────────────────────────────────────────────────────

        private LootOptionUI[] _options;

        private readonly List<int> _selectionOrder = new();

        // Effective m, clamped to actual rune count each Show().
        private int _effectiveMax;

        private bool _isShowing;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirm);
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RoomManager.RoomClearEvent>(OnRoomCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoomManager.RoomClearEvent>(OnRoomCleared);
        }

        // ── Event handler ─────────────────────────────────────────────────────

        private void OnRoomCleared(RoomManager.RoomClearEvent evt)
        {
            if (_isShowing)
                return;

            Show();
        }

        // ── Show / Hide ───────────────────────────────────────────────────────

        private void Show()
        {
            if (_dropPool == null)
            {
                Debug.LogWarning("[LootSelectionUI] No PickupDropPool assigned.");
                return;
            }

            _isShowing = true;
            _selectionOrder.Clear();

            RuneDefinitionSO[] runes = _dropPool.GetRandomRunes(_runesToShow);
            _effectiveMax = Mathf.Min(_runesToSelect, runes.Length);

            // Clear any previously spawned option tiles.
            foreach (Transform child in _runeContainer)
                Destroy(child.gameObject);

            _options = new LootOptionUI[runes.Length];

            for (int i = 0; i < runes.Length; i++)
            {
                int captured = i;
                LootOptionUI option = Instantiate(_lootOptionPrefab, _runeContainer);
                option.Init(runes[i], () => OnOptionClicked(captured));
                _options[i] = option;
            }

            _panel.SetActive(true);
            Time.timeScale = 0f;
            Helpers.Input.EnableUIInput();
        }

        private void Hide()
        {
            _panel.SetActive(false);
            _selectionOrder.Clear();
            _isShowing = false;
            Time.timeScale = 1f;
            Helpers.Input.EnablePlayerInput();
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
                    _options[evicted].SetSelected(false);
                }

                _selectionOrder.Add(index);
            }

            _options[index].SetSelected(_selectionOrder.Contains(index));
        }

        // ── Confirm ───────────────────────────────────────────────────────────

        private void OnConfirm()
        {
            // 0 selected is a valid no-op — loop simply doesn't execute.
            foreach (int index in _selectionOrder)
                GameStateManager.RunState.AddRune(_options[index].Rune);

            Hide();
        }
    }
}