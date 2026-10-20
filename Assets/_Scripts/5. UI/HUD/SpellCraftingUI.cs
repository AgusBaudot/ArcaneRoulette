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
    public sealed class SpellCraftingUI : BaseUIPanel
    {
        [Header("Slot Panels — assign First, Second, Third in fixed order")] [SerializeField]
        private SpellSlotPanel[] _slotPanels; // always length 3

        [Header("Carousel Navigation")] [SerializeField]
        private Button _leftArrowButton;

        [SerializeField] private Button _rightArrowButton;

        [Header("Overlay — sits between center and back panels")] [SerializeField]
        private GameObject _dimOverlay;

        [Header("Inventory")] [SerializeField] private RuneInventoryPanel _inventoryPanel;

        [Header("Filter Tabs")] [SerializeField]
        private Button _btnFilterAll;

        [SerializeField] private Button _btnFilterAbility;
        [SerializeField] private Button _btnFilterElement;
        [SerializeField] private Button _btnFilterCast;
        [SerializeField] private Button _btnFilterOnHit;
        [SerializeField] private RuneFilterTab[] _filterTabs;

        [Header("Carousel Animation")] [SerializeField]
        private float _animDuration = 0.35f;

        [SerializeField] private Ease _animEase = Ease.OutCubic;

        private TooltipSystem _tooltip => TooltipSystem.Instance;


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
        private int _centerIndex; // which _slotPanels[] index is currently centered
        private RuneFilter _currentFilter = RuneFilter.All;
        private RectTransform _pendingRuneRect;
        private float _comboShakeIntensity = 0f;
        private float _lastHitTime = 0f;

        // ── Unity ────────────────────────────────────────────────────────────

        #region Unity

        private void Awake()
        {
            base.Awake();

            _spellCrafter = FindObjectOfType<SpellCrafter>();

            _inventoryPanel.Init(this);
            foreach (var panel in _slotPanels)
            {
                panel.Init(this);
            }

            _leftArrowButton.onClick.AddListener(OnLeftArrow);
            _rightArrowButton.onClick.AddListener(OnRightArrow);

            foreach (var tab in _filterTabs)
            {
                tab.Init(OnFilterTabClicked);
            }
        }

        #endregion

        // ── Show / Hide ────────────────────────────────────────────────────

        public override void Show()
        {
            base.Show();

            _pendingRune = null;
            _pendingRuneIndex = -1;

            Helpers.Input.OnCarouselLeft += OnLeftArrow;
            Helpers.Input.OnCarouselRight += OnRightArrow;
            Helpers.Input.OnFilterPrevious += OnFilterPrevious;
            Helpers.Input.OnFilterNext += OnFilterNext;

            if (_tooltip != null)
            {
                Helpers.Input.OnToggleTooltip += _tooltip.ToggleEnabled;
            }

            foreach (var panel in _slotPanels)
            {
                panel.PopulateFromRunState();
            }

            _inventoryPanel.Rebuild(_currentFilter, GetEffectiveAvailableCount);

            foreach (var panel in _slotPanels)
            {
                panel.VisualRoot.DOKill();
                panel.VisualRoot.offsetMin = Vector2.zero;
                panel.VisualRoot.offsetMax = Vector2.zero;
                panel.VisualRoot.localScale = Vector3.one;
            }

            ApplyCarouselLayout();
            RefreshAll();
            RefreshTabVisuals();
            EventSystem.current.SetSelectedGameObject(null);
        }

        public override void Hide()
        {
            base.Hide();

            _tooltip?.Hide();
            _pendingRune = null;
            _pendingRuneIndex = -1;

            Helpers.Input.OnCarouselLeft -= OnLeftArrow;
            Helpers.Input.OnCarouselRight -= OnRightArrow;
            Helpers.Input.OnFilterPrevious -= OnFilterPrevious;
            Helpers.Input.OnFilterNext -= OnFilterNext;

            if (_tooltip != null)
            {
                Helpers.Input.OnToggleTooltip -= _tooltip.ToggleEnabled;
            }
        }

        // ── Arrow navigation ─────────────────────────────────────────────────

        // Right arrow: the right back slot comes to center.
        private void OnRightArrow()
        {
            EventBus.Publish(new AudioPlayRequest
            {
                Event = Helpers.UIAudio.CarouselSlide
            });

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
            EventBus.Publish(new AudioPlayRequest
            {
                Event = Helpers.UIAudio.CarouselSlide
            });

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
            int count = _slotPanels.Length;

            for (int i = 0; i < count; i++)
            {
                int offset = (i - _centerIndex + count) % count;
                var panel = _slotPanels[i];
                var rect = _slotPanels[i].VisualRoot;

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

            if (_dimOverlay != null)
            {
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

        public void OnInventoryTileClicked(RuneDefinitionSO rune, int index, PointerEventData.InputButton buttonType,
            RectTransform clickedRect)
        {
            if (buttonType == PointerEventData.InputButton.Left)
            {
                bool autoAssigned = _slotPanels[_centerIndex]
                    .TryAutoAssignEmptySlot(rune, out var slotType, out var modIndex);

                if (autoAssigned)
                {
                    _slotPanels[_centerIndex].TryApply(_spellCrafter);

                    RectTransform targetRect = _slotPanels[_centerIndex].GetTileRect(slotType, modIndex);

                    AnimateRuneFlight(rune, clickedRect, targetRect);

                    _inventoryPanel.RemoveOneTile(rune);

                    if (_pendingRuneIndex == index)
                    {
                        _pendingRune = null;
                        _pendingRuneIndex = -1;
                        _pendingRuneRect = null;
                    }
                }
                else
                {
                    EventBus.Publish(new AudioPlayRequest { Event = Helpers.UIAudio.RuneFail });
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
                    panel.TryApply(_spellCrafter);

                    RectTransform targetRect = panel.GetTileRect(slotType, modIndex);
                    AnimateRuneFlight(_pendingRune, _pendingRuneRect, targetRect);

                    _inventoryPanel.RemoveOneTile(_pendingRune);

                    if (replacedRune != null)
                        _inventoryPanel.AddOneTile(replacedRune);
                }
                else
                {
                    EventBus.Publish(new AudioPlayRequest { Event = Helpers.UIAudio.RuneFail });
                }
            }
            else
            {
                RuneDefinitionSO clearedRune = panel.ClearSlot(slotType, modIndex);

                if (clearedRune != null)
                {
                    panel.TryApply(_spellCrafter);

                    EventBus.Publish(new AudioPlayRequest
                    {
                        Event = Helpers.UIAudio.RuneUnequip
                    });

                    _inventoryPanel.AddOneTile(clearedRune);

                    // If the cleared rune doesn't match the current filter, switch to its filter
                    if (!RuneMatchesCurrentFilter(clearedRune))
                    {
                        _currentFilter = GetFilterForRune(clearedRune);
                        _inventoryPanel.Rebuild(_currentFilter, GetEffectiveAvailableCount);
                        RefreshTabVisuals();
                    }
                }
            }

            _pendingRune = null;
            _pendingRuneIndex = -1;
            RefreshAll();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnFilterPrevious() => ShiftFilter(-1);
        private void OnFilterNext() => ShiftFilter(1);

        private void ShiftFilter(int direction)
        {
            if (_filterTabs == null || _filterTabs.Length == 0)
                return;

            int currentIndex = 0;
            for (int i = 0; i < _filterTabs.Length; i++)
            {
                if (_filterTabs[i].FilterType == _currentFilter)
                {
                    currentIndex = i;
                    break;
                }
            }

            int newIndex = (currentIndex + direction + _filterTabs.Length) % _filterTabs.Length;
            OnFilterTabClicked(_filterTabs[newIndex]);
        }

        private void OnFilterTabClicked(RuneFilterTab clickedTab)
        {
            if (_currentFilter == clickedTab.FilterType)
                return;

            EventBus.Publish(new AudioPlayRequest
            {
                Event = Helpers.UIAudio.TabSwitch
            });

            _currentFilter = clickedTab.FilterType;
            _inventoryPanel.Rebuild(_currentFilter, GetEffectiveAvailableCount);
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
            foreach (var tab in _filterTabs)
                tab.SetActiveState(tab.FilterType == _currentFilter);
        }

        private int GetEffectiveAvailableCount(RuneDefinitionSO rune)
        {
            var selectedCounts = GetSelectedRuneCounts();
            var committedCounts = GetCommittedRuneCounts();

            selectedCounts.TryGetValue(rune, out int selected);
            committedCounts.TryGetValue(rune, out int committed);

            return GameStateManager.RunState.AvailableCount(rune) + committed - selected;
        }

        private Dictionary<RuneDefinitionSO, int> GetSelectedRuneCounts()
        {
            var counts = new Dictionary<RuneDefinitionSO, int>();
            foreach (var panel in _slotPanels)
                panel.AccumulateSelectedRunes(counts);
            return counts;
        }

        private Dictionary<RuneDefinitionSO, int> GetCommittedRuneCounts()
        {
            var counts = new Dictionary<RuneDefinitionSO, int>();
            foreach (var panel in _slotPanels)
            {
                var current = GameStateManager.RunState.GetSlot(panel.TargetSlot) as SpellInstance;
                if (current == null)
                    continue;

                AccumulateRuneCount(counts, current.Recipe.Ability);
                AccumulateRuneCount(counts, current.Recipe.Element);
                foreach (var modifier in current.Recipe.Modifiers)
                    AccumulateRuneCount(counts, modifier);
            }

            return counts;
        }

        private static void AccumulateRuneCount(Dictionary<RuneDefinitionSO, int> counts, RuneDefinitionSO rune)
        {
            if (rune == null)
                return;

            counts.TryGetValue(rune, out int current);
            counts[rune] = current + 1;
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

        private void AnimateRuneFlight(RuneDefinitionSO rune, RectTransform startRect, RectTransform targetRect)
        {
            if (startRect == null || targetRect == null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = Helpers.UIAudio.RuneEquip });
                return;
            }

            CanvasGroup targetCanvasGroup = targetRect.GetComponent<CanvasGroup>();
            if (targetCanvasGroup == null)
                targetCanvasGroup = targetRect.gameObject.AddComponent<CanvasGroup>();

            targetCanvasGroup.alpha = 0f;
            targetCanvasGroup.blocksRaycasts = false;

            GameObject dummy = new GameObject("FlyingRuneDummy");
            RectTransform dummyRect = dummy.AddComponent<RectTransform>();
            dummyRect.SetParent(this.transform, true);
            dummyRect.SetAsLastSibling();

            Image img = dummy.AddComponent<Image>();
            img.sprite = rune.Icon;
            img.preserveAspect = true;
            img.raycastTarget = false;

            dummyRect.position = startRect.position;
            dummyRect.sizeDelta = startRect.rect.size * rune.UIIconScale;
            dummyRect.localScale = startRect.lossyScale;

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);

            seq.Append(dummyRect.DOMove(targetRect.position, 0.25f).SetEase(Ease.InBack));
            seq.Join(dummyRect.DOScale(dummyRect.localScale * 1.3f, 0.12f).SetLoops(2, LoopType.Yoyo));

            seq.OnComplete(() =>
            {
                Destroy(dummy);

                targetCanvasGroup.alpha = 1f;
                targetCanvasGroup.blocksRaycasts = true;

                if (Time.unscaledTime - _lastHitTime > 0.5f)
                {
                    _comboShakeIntensity = 0f;
                }

                _lastHitTime = Time.unscaledTime;

                _comboShakeIntensity = Mathf.Min(_comboShakeIntensity + 15f, 60f);

                Vector2 uiPunchForce = new Vector2(0f, -_comboShakeIntensity);

                if (_comboShakeIntensity > 15f)
                {
                    float xDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                    uiPunchForce.x = _comboShakeIntensity * 0.75f * xDirection;
                }

                RectTransform centerPanelRect = _slotPanels[_centerIndex].VisualRoot;

                centerPanelRect.DOKill(true);
                centerPanelRect.DOPunchAnchorPos(uiPunchForce, 0.35f, 25, 1f).SetUpdate(true);

                float cameraTrauma = 0.15f + (_comboShakeIntensity / 60f) * 0.25f;
                GameFeelSystem.ApplyCameraShake(cameraTrauma);

                EventBus.Publish(new AudioPlayRequest { Event = Helpers.UIAudio.RuneEquip });

                targetRect.DOKill(true);
                targetRect.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 1f).SetUpdate(true);
            });
        }
    }
}