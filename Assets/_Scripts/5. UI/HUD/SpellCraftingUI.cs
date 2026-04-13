using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Foundation;
using Core;

namespace UI
{
    public sealed class SpellCraftingUI : MonoBehaviour
    {
        public static bool IsUIOpen => _isOpen;
        
        [Header("Panel")]
        [SerializeField] private GameObject _craftingPanel;
        [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;

        [Header("Slot Panels — assign First, Second, Third in fixed order")] 
        [SerializeField] private SpellSlotPanel[] _slotPanels; // always length 3

        [Header("Carousel Navigation")]
        [SerializeField] private Button _leftArrowButton;

        [SerializeField] private Button _rightArrowButton;

        [Header("Overlay — sits between center and back panels")] 
        [SerializeField] private GameObject _dimOverlay;

        [Header("Inventory")]
        [SerializeField] private RuneInventoryPanel _inventoryPanel;
        
        [Header("Filter Tabs")]
        [SerializeField] private Button _btnFilterAll;
        [SerializeField] private Button _btnFilterAbility;
        [SerializeField] private Button _btnFilterElement;
        [SerializeField] private Button _btnFilterCast;
        [SerializeField] private Button _btnFilterOnHit;
        [SerializeField] private RuneFilterTab[] _filterTabs;

        [Header("Carousel Animation")] 
        [SerializeField] private float _animDuration = 0.35f;
        [SerializeField] private Ease _animEase = Ease.OutCubic;

        // ── Carousel layout constants ────────────────────────────────────────
        // Center
        private static readonly Vector2 CenterOffsetMin = Vector2.zero;
        private static readonly Vector2 CenterOffsetMax = Vector2.zero;
        private static readonly Vector3 CenterScale = Vector3.one;

        // Left back slot
        private static readonly Vector2 LeftOffsetMin = new(-75f, -25f);
        private static readonly Vector2 LeftOffsetMax = new(-75f, -25f);
        private static readonly Vector3 BackScale = new(0.75f, 0.75f, 1f);

        // Right back slot
        private static readonly Vector2 RightOffsetMin = new(300f, -25f);
        private static readonly Vector2 RightOffsetMax = new(300f, -25f);

        // ── Runtime state ────────────────────────────────────────────────────
        private SpellCrafter _spellCrafter;
        private RuneDefinitionSO _pendingRune;
        private int _pendingRuneIndex = -1;
        private static bool _isOpen;
        private int _centerIndex; // which _slotPanels[] index is currently centered
        private RuneFilter _currentFilter = RuneFilter.All;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _spellCrafter = FindObjectOfType<SpellCrafter>();

            _inventoryPanel.Init(this);
            foreach (var panel in _slotPanels)
                panel.Init(this);

            _leftArrowButton.onClick.AddListener(OnLeftArrow);
            _rightArrowButton.onClick.AddListener(OnRightArrow);
            
            foreach(var tab in _filterTabs)
                tab.Init(OnFilterTabClicked);
            
            _craftingPanel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                if (_isOpen) CloseCraftingUI();
                else OpenCraftingUI();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && _isOpen)
                CloseCraftingUI();
        }

        // ── Open / Close ─────────────────────────────────────────────────────

        private void OpenCraftingUI()
        {
            _isOpen = true;
            _pendingRune = null;
            _pendingRuneIndex = -1;

            _craftingPanel.SetActive(true);
            Time.timeScale = 0f;

            foreach (var panel in _slotPanels)
                panel.PopulateFromRunState();
            
            _inventoryPanel.Rebuild(_currentFilter);
            ApplyCarouselLayout();
            RefreshAll();
            RefreshTabVisuals();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void CloseCraftingUI()
        {
            _isOpen = false;
            _pendingRune = null;
            _pendingRuneIndex = -1;

            foreach (var panel in _slotPanels)
                panel.TryApply(_spellCrafter);

            _craftingPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        // ── Arrow navigation ─────────────────────────────────────────────────

        // Right arrow: the right back slot comes to center.
        // [A, B, C] center=B → right pressed → center=C → visual order: B(left), C(center), A(right)
        private void OnRightArrow()
        {
            _centerIndex = (_centerIndex + 1) % _slotPanels.Length;
            _pendingRune = null;
            _pendingRuneIndex = -1;
            ApplyCarouselLayout();
            RefreshAll();
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Left arrow: the left back slot comes to center.
        private void OnLeftArrow()
        {
            _centerIndex = (_centerIndex + _slotPanels.Length - 1) % _slotPanels.Length;
            _pendingRune = null;
            _pendingRuneIndex = -1;
            ApplyCarouselLayout();
            RefreshAll();
            EventSystem.current.SetSelectedGameObject(null);
        }

        // ── Carousel layout ───────────────────────────────────────────────────

        /// <summary>
        /// Repositions and rescales all three panels instantly.
        /// Center panel: full size, interactable.
        /// Left back: offset left, scaled down, not interactable.
        /// Right back: offset right, scaled down, not interactable.
        /// </summary>
        private void ApplyCarouselLayout()
        {
            int count = _slotPanels.Length; // 3

            for (int i = 0; i < count; i++)
            {
                int offset = (i - _centerIndex + count) % count;
                var panel = _slotPanels[i];
                var rect = _slotPanels[i].VisualRoot;

                // 1. Kill any running tweens on this specific RectTransform
                rect.DOKill();

                bool isCenter = offset == 0;
                bool isRight = offset == 1;

                Vector2 targetMin;
                Vector2 targetMax;
                Vector3 targetScale;

                if (isCenter)
                {
                    targetMin = CenterOffsetMin;
                    targetMax = CenterOffsetMax;
                    targetScale = CenterScale;
                    panel.SetInteractable(true);
                }
                else if (isRight)
                {
                    targetMin = RightOffsetMin;
                    targetMax = RightOffsetMax;
                    targetScale = BackScale;
                    panel.SetInteractable(false);
                }
                else // left back
                {
                    targetMin = LeftOffsetMin;
                    targetMax = LeftOffsetMax;
                    targetScale = BackScale;
                    panel.SetInteractable(false);
                }

                // 2. Animate using DOTween. 
                // SetTarget ensures DOKill works later. SetUpdate(true) ensures it runs when timeScale == 0.
                DOTween.To(() => rect.offsetMin, x => rect.offsetMin = x, targetMin, _animDuration)
                    .SetEase(_animEase)
                    .SetTarget(rect)
                    .SetUpdate(true);

                DOTween.To(() => rect.offsetMax, x => rect.offsetMax = x, targetMax, _animDuration)
                    .SetEase(_animEase)
                    .SetTarget(rect)
                    .SetUpdate(true);

                rect.DOScale(targetScale, _animDuration)
                    .SetEase(_animEase)
                    .SetUpdate(true);
            }

            // 3. EXACTLY preserve your sibling order logic on the PANEL roots, not the VisualRoots.
            if (_dimOverlay != null)
            {
                // Back panels first (arbitrary order), then overlay, then center on top.
                for (int i = 0; i < count; i++)
                {
                    int offset = (i - _centerIndex + count) % count;
                    if (offset != 0)
                        _slotPanels[i].transform.SetAsFirstSibling();
                }

                _dimOverlay.transform.SetSiblingIndex(count - 1);
                _slotPanels[_centerIndex].transform.SetAsLastSibling();
            }
        }

        // ── Click callbacks ───────────────────────────────────────────────────

        public void OnInventoryTileClicked(RuneDefinitionSO rune, int index, PointerEventData.InputButton buttonType)
        {
            if (buttonType == PointerEventData.InputButton.Right)
            {
                //Right click: Auto-assign
                bool autoAssigned = _slotPanels[_centerIndex].TryAutoAssignEmptySlot(rune);

                if (autoAssigned)
                {
                    _inventoryPanel.RemoveOneTile(rune);

                    if (_pendingRuneIndex == index)
                    {
                        _pendingRune = null;
                        _pendingRuneIndex = -1;
                    }
                }
            }
            else if (buttonType == PointerEventData.InputButton.Left)
            {
                //Left click: Select/Deselect
                if (_pendingRuneIndex == index)
                {
                    //Clicked the already-selected rune -> Deselect it
                    _pendingRune = null;
                    _pendingRuneIndex = -1;
                }
                else
                {
                    //Clicked a new rune -> Select it
                    _pendingRune = rune;
                    _pendingRuneIndex = index;
                }
            }
            
            RefreshAll();
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void OnSlotTileClicked(SpellSlotPanel panel,
            SpellSlotPanel.SlotType slotType,
            int modIndex)
        {
            if (_pendingRune != null)
            {
                bool assigned = panel.TryAssign(_pendingRune, slotType, modIndex, out var replacedRune);
                if (assigned)
                {
                    _inventoryPanel.RemoveOneTile(_pendingRune);
                    
                    if (replacedRune != null)
                        _inventoryPanel.AddOneTile(replacedRune);
                }
            }
            else
            {
                RuneDefinitionSO clearedRune = panel.ClearSlot(slotType, modIndex);
                
                if (clearedRune != null)
                {
                    _inventoryPanel.AddOneTile(clearedRune);
                    
                    // If the cleared rune doesn't match the current filter, switch to its filter
                    if (!RuneMatchesCurrentFilter(clearedRune))
                    {
                        _currentFilter = GetFilterForRune(clearedRune);
                        _inventoryPanel.Rebuild(_currentFilter);
                        RefreshTabVisuals();
                    }
                }
            }

            _pendingRune = null;
            _pendingRuneIndex = -1;
            RefreshAll();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnFilterTabClicked(RuneFilterTab clickedTab)
        {
            if (_currentFilter == clickedTab.FilterType)
                return;
            
            _currentFilter = clickedTab.FilterType;
            _inventoryPanel.Rebuild(_currentFilter);
            RefreshTabVisuals();
            EventSystem.current.SetSelectedGameObject(null);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            Func<int, bool> highlight = index => index == _pendingRuneIndex && index != -1;

            _inventoryPanel.Refresh(highlight);
            
            foreach (var panel in _slotPanels)
                panel.RefreshDisplay(_ => false);
        }
        
        private void RefreshTabVisuals()
        {
            foreach(var tab in _filterTabs)
            tab.SetActiveState(tab.FilterType == _currentFilter);
        }

        private RuneFilter GetFilterForRune(RuneDefinitionSO rune)
        {
            if (rune is AbilityRuneSO) return RuneFilter.Ability;
            if (rune is ElementRuneSO) return RuneFilter.Element;
            if (rune is CastRuneSO) return RuneFilter.Cast;
            if (rune is OnHitRuneSO) return RuneFilter.OnHit;
            return RuneFilter.All; // fallback
        }

        private bool RuneMatchesCurrentFilter(RuneDefinitionSO rune)
        {
            if (_currentFilter == RuneFilter.All) return true;
            return GetFilterForRune(rune) == _currentFilter;
        }
    }
}