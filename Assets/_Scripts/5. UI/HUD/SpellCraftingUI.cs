using System;
using UnityEngine;
using UnityEngine.UI;
using Foundation;
using Core;

public sealed class SpellCraftingUI : MonoBehaviour
{
    [Header("Panel")] 
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Slot Panels — assign First, Second, Third in fixed order")] [SerializeField]
    private SpellSlotPanel[] slotPanels; // always length 3

    [Header("Carousel Navigation")] [SerializeField]
    private Button leftArrowButton;

    [SerializeField] private Button rightArrowButton;

    [Header("Overlay — sits between center and back panels")] [SerializeField]
    private GameObject dimOverlay;

    [Header("Inventory")] [SerializeField] private RuneInventoryPanel inventoryPanel;

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
    private bool _isOpen;
    private int _centerIndex; // which slotPanels[] index is currently centered

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _spellCrafter = FindObjectOfType<SpellCrafter>();

        inventoryPanel.Init(this);
        foreach (var panel in slotPanels)
            panel.Init(this);

        leftArrowButton.onClick.AddListener(OnLeftArrow);
        rightArrowButton.onClick.AddListener(OnRightArrow);

        craftingPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
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

        craftingPanel.SetActive(true);
        Time.timeScale = 0f;

        foreach (var panel in slotPanels)
            panel.PopulateFromRunState();

        ApplyCarouselLayout();
        RefreshAll();
    }

    private void CloseCraftingUI()
    {
        _isOpen = false;
        _pendingRune = null;

        foreach (var panel in slotPanels)
            panel.TryApply(_spellCrafter);

        craftingPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // ── Arrow navigation ─────────────────────────────────────────────────

    // Right arrow: the right back slot comes to center.
    // [A, B, C] center=B → right pressed → center=C → visual order: B(left), C(center), A(right)
    private void OnRightArrow()
    {
        _centerIndex = (_centerIndex + 1) % slotPanels.Length;
        _pendingRune = null;
        ApplyCarouselLayout();
        RefreshAll();
    }

    // Left arrow: the left back slot comes to center.
    private void OnLeftArrow()
    {
        _centerIndex = (_centerIndex + slotPanels.Length - 1) % slotPanels.Length;
        _pendingRune = null;
        ApplyCarouselLayout();
        RefreshAll();
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
        int count = slotPanels.Length; // 3

        // Relative positions around center:
        // offset 0 = center, offset 1 = right back, offset -1 (= 2) = left back
        for (int i = 0; i < count; i++)
        {
            int offset = (i - _centerIndex + count) % count;
            // offset 0 = center, 1 = right, 2 = left
            var panel = slotPanels[i];
            var rect = slotPanels[i].VisualRoot;

            bool isCenter = offset == 0;
            bool isRight = offset == 1;
            // offset == 2 → left back

            if (isCenter)
            {
                rect.offsetMin = CenterOffsetMin;
                rect.offsetMax = CenterOffsetMax;
                rect.localScale = CenterScale;
                panel.SetInteractable(true);
            }
            else if (isRight)
            {
                rect.offsetMin = RightOffsetMin;
                rect.offsetMax = RightOffsetMax;
                rect.localScale = BackScale;
                panel.SetInteractable(false);
            }
            else // left back
            {
                rect.offsetMin = LeftOffsetMin;
                rect.offsetMax = LeftOffsetMax;
                rect.localScale = BackScale;
                panel.SetInteractable(false);
            }
        }

        // Dim overlay sits between center and back panels in the hierarchy.
        // No repositioning needed — it's a full-stretch sibling.
        // Ensure sibling order: back panels → dim overlay → center panel.
        // We do this by setting the center panel as the last sibling.
        if (dimOverlay != null)
        {
            // Back panels first (arbitrary order), then overlay, then center on top.
            for (int i = 0; i < count; i++)
            {
                int offset = (i - _centerIndex + count) % count;
                if (offset != 0)
                    slotPanels[i].transform.SetAsFirstSibling();
            }

            dimOverlay.transform.SetSiblingIndex(count - 1);
            slotPanels[_centerIndex].transform.SetAsLastSibling();
        }
    }

    // ── Click callbacks ───────────────────────────────────────────────────

    public void OnInventoryTileClicked(RuneDefinitionSO rune)
    {
        _pendingRune = (_pendingRune == rune) ? null : rune;
        RefreshAll();
    }

    public void OnSlotTileClicked(SpellSlotPanel panel,
        SpellSlotPanel.SlotType slotType,
        int modIndex)
    {
        if (_pendingRune != null)
            panel.TryAssign(_pendingRune, slotType, modIndex);
        else
            panel.ClearSlot(slotType, modIndex);

        _pendingRune = null;
        RefreshAll();
    }

    // ── Refresh ───────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        Func<RuneDefinitionSO, bool> highlight = rune => rune == _pendingRune;

        inventoryPanel.Refresh(highlight);
        foreach (var panel in slotPanels)
            panel.RefreshDisplay(_ => false);
    }
}