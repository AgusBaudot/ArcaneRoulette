using UnityEngine;
using UnityEngine.UI;
using Foundation;
using Core;

/// <summary>
/// Orchestrator. Owns the pending rune selection.
/// Opens/closes the panel, routes clicks, commits on close.
/// </summary>
public sealed class SpellCraftingUI : MonoBehaviour
{
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [SerializeField] private RuneInventoryPanel inventoryPanel;
    [SerializeField] private SpellSlotPanel[] slotPanels; // length 3, Slot0/1/2

    private SpellCrafter _spellCrafter;
    private RuneDefinitionSO _pendingRune;
    private bool _isOpen;

    private void Awake()
    {
        _spellCrafter = FindObjectOfType<SpellCrafter>();

        inventoryPanel.Init(this);

        foreach (var panel in slotPanels)
            panel.Init(this);

        craftingPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_isOpen) CloseCraftingUI();
            else         OpenCraftingUI();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _isOpen)
            CloseCraftingUI();
    }

    // ── Open / Close ────────────────────────────────────────────────────

    private void OpenCraftingUI()
    {
        _isOpen = true;
        _pendingRune = null;

        craftingPanel.SetActive(true);
        Time.timeScale = 0f;

        foreach (var panel in slotPanels)
            panel.PopulateFromRunState();

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

    // ── Click callbacks (called by tiles via Init lambdas) ──────────────

    /// <summary>
    /// Inventory tile clicked. Toggles pending selection.
    /// </summary>
    public void OnInventoryTileClicked(RuneDefinitionSO rune)
    {
        _pendingRune = (_pendingRune == rune) ? null : rune;
        RefreshAll();
    }

    /// <summary>
    /// Spell slot tile clicked.
    /// If holding a rune: assign it (type mismatch = ignored by TryAssign).
    /// If not holding: clear that slot.
    /// </summary>
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

    // ── Refresh ─────────────────────────────────────────────────────────

    /// <summary>
    /// Single refresh call updates all tiles on both sides.
    /// Called after every state change.
    /// </summary>
    private void RefreshAll()
    {
        inventoryPanel.Refresh(_pendingRune);

        foreach (var panel in slotPanels)
            panel.RefreshDisplay(_pendingRune);
    }
}