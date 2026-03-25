using UnityEngine;
using UnityEngine.UI;
using Foundation;
using System.Collections.Generic;
using Core;
using System;

public class SpellCraftingUI : MonoBehaviour
{
    [SerializeField] private GameObject mainInventorySide;
    [SerializeField] private GameObject craftingSide;
    [SerializeField] private Transform inventoryGridParent;
    [SerializeField] private GameObject runeSlotPrefab;
    [SerializeField] private CraftingRecipePanel[] craftingPanels = new CraftingRecipePanel[3];
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    private VolatileRunState RunState => GameStateManager.RunState;
    private SpellCrafter _spellCrafter;
    private List<RuneSlotUI> _inventorySlots = new List<RuneSlotUI>();
    private bool _uiOpen = false;

    private void Awake()
    {
        _spellCrafter = FindObjectOfType<SpellCrafter>();
        
        if (openButton != null)
            openButton.onClick.AddListener(OpenCraftingUI);

        if (openButton != null)
            openButton.onClick.AddListener(OpenCraftingUI);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        // Find crafting panels if not assigned
        if (craftingPanels.Length == 0 || craftingPanels[0] == null)
        {
            craftingPanels = FindObjectsOfType<CraftingRecipePanel>();
        }

        EnsureUniquePanelTargetSlots();

        // Fallback 1: use assigned craftingSide if set.
        if (craftingPanel == null && craftingSide != null)
        {
            craftingPanel = craftingSide;
            Debug.LogWarning("SpellCraftingUI: craftingPanel was null, auto-assigned craftingSide as craftingPanel.");
        }

        // Fallback 2: use this GameObject to avoid complete null fail.
        if (craftingPanel == null)
        {
            craftingPanel = gameObject;
            Debug.LogWarning("SpellCraftingUI: craftingPanel was null, auto-assigned to component GameObject.");
        }

        // Instant debug info on startup.
        Debug.Log($"SpellCraftingUI Awake: craftPanel={craftingPanel?.name} craftSide={craftingSide?.name} openBtn={(openButton!=null)} closeBtn={(closeButton!=null)} inventoryGrid={(inventoryGridParent!=null)}");
    }

    private void EnsureUniquePanelTargetSlots()
    {
        if (craftingPanels == null || craftingPanels.Length == 0)
            return;

        // If panels share the same SlotIndex (common misconfiguration), they will all mirror the same spell.
        // Auto-assign by index (0..2) in that case to keep UI sane.
        var seen = new HashSet<SlotIndex>();
        bool hasDuplicate = false;

        foreach (var p in craftingPanels)
        {
            if (p == null) continue;
            if (!seen.Add(p.TargetSlot))
            {
                hasDuplicate = true;
                break;
            }
        }

        if (!hasDuplicate)
            return;

        Debug.LogWarning("SpellCraftingUI: CraftingRecipePanels have duplicate TargetSlot values. Auto-assigning Slot0/Slot1/Slot2 by panel index.");

        for (int i = 0; i < craftingPanels.Length; i++)
        {
            var p = craftingPanels[i];
            if (p == null) continue;
            if (i <= 2)
                p.SetTargetSlot((SlotIndex)i);
        }
    }

    private void Start()
    {
        Debug.Log("SpellCraftingUI: START. component active=" + enabled + ", gameObject active=" + gameObject.activeSelf);

        // Ensure UI starts closed
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
            Debug.Log("SpellCraftingUI: Start() set craftingPanel inactive.");
        }
        else
        {
            Debug.LogError("SpellCraftingUI: craftingPanel is still null in Start(). UI cannot open.");
        }

        // Safety check: if open button is missing, log explicitly.
        if (openButton == null)
            Debug.LogWarning("SpellCraftingUI: openButton is not assigned. Use 'C' key or create open button in Canvas.");

        // Safety test: show UI by direct call once at start while debugging (comment out after verification)
        // OpenCraftingUI();
    }

    private void Update()
    {
        // Extra key fail-safe (Tab can be blocked by UI/input focus or New Input System setup).
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"SpellCraftingUI: Toggle key pressed (toggleKey={toggleKey}). _uiOpen={_uiOpen}");
            if (_uiOpen)
            {
                OnCloseButtonClicked();
            }
            else
            {
                OpenCraftingUI();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("SpellCraftingUI: Escape pressed, trying close UI.");
            if (_uiOpen)
                CloseCraftingUI();
        }
    }

    public void OpenCraftingUI()
    {
        Debug.Log("SpellCraftingUI: OpenCraftingUI invoked (toggleKey=" + toggleKey + "). _uiOpen=" + _uiOpen);

        if (_uiOpen)
            return;

        _uiOpen = true;
        EnsureUniquePanelTargetSlots();

        // Show UI
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(true);
            Debug.Log("SpellCraftingUI: Crafting panel set active.");
        }
        else
        {
            Debug.LogError("SpellCraftingUI: craftingPanel is null in OpenCraftingUI.");
        }

        // Pause game
        Time.timeScale = 0f;
        Debug.Log("SpellCraftingUI: game paused (timeScale=0). UI should be visible.");

        // Refresh inventory display
        RefreshInventoryDisplay();

        // Refresh crafting panels
        foreach (var panel in craftingPanels)
        {
            if (panel != null)
            {
                panel.SetRuneSlotPrefab(runeSlotPrefab);
                panel.PopulateFromCurrentSlot();
                panel.RefreshDisplay(_spellCrafter);
            }
        }

        Debug.Log("SpellCraftingUI: OpenCraftingUI completed.");
    }

    private void OnCloseButtonClicked()
    {
        CloseCraftingUI();
    }

    public void CloseCraftingUI()
    {
        if (!_uiOpen)
            return;

        _uiOpen = false;

        ApplySelectionsToRunState();
        
        // Hide UI
        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        // Resume game
        Time.timeScale = 1f;
    }

    public void NotifyCraftSuccessful()
    {
        // Update remaining rune counts after successful crafting.
        RefreshInventoryDisplay();

        foreach (var panel in craftingPanels)
        {
            if (panel != null)
            {
                panel.PopulateFromCurrentSlot();
                panel.RefreshDisplay(_spellCrafter);
            }
        }
    }

    private void ApplySelectionsToRunState()
    {
        if (_spellCrafter == null) return;

        foreach (var panel in craftingPanels)
        {
            if (panel == null) continue;
            var ok = panel.TryApplySelectionToRunState(_spellCrafter);
            if (!ok)
                Debug.LogWarning($"SpellCraftingUI: Failed to apply selection for slot {panel.TargetSlot}.");
        }

        // Sync visuals from runtime after applying.
        RefreshInventoryDisplay();
        foreach (var panel in craftingPanels)
        {
            if (panel == null) continue;
            panel.PopulateFromCurrentSlot();
            panel.RefreshDisplay(_spellCrafter);
        }
    }

    public void RefreshInventoryDisplay()
    {
        // Clear existing slots
        foreach (var slot in _inventorySlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        _inventorySlots.Clear();

        if (inventoryGridParent == null)
            return;

        // Populate with available runes
        foreach (var runeEntry in RunState.RuneInventory)
        {
            int available = RunState.AvailableCount(runeEntry.Key);

            GameObject slotGO = null;
            if (runeSlotPrefab != null)
            {
                slotGO = Instantiate(runeSlotPrefab, inventoryGridParent);
            }
            else
            {
                var resourcePrefab = Resources.Load<GameObject>("RuneSlot");
                if (resourcePrefab != null)
                    slotGO = Instantiate(resourcePrefab, inventoryGridParent);
            }

            if (slotGO == null)
            {
                slotGO = new GameObject(runeEntry.Key.Name);
                slotGO.transform.SetParent(inventoryGridParent);
            }

            var slotUI = slotGO.GetComponent<RuneSlotUI>() ?? slotGO.AddComponent<RuneSlotUI>();
            slotUI.SetAsInventorySlot();
            // Disable click-to-assign; drag/drop only.
            slotUI.Setup(runeEntry.Key, available, null);

            // Drag (inventory -> right slots). Drop (right slots -> inventory) is handled per-tile.
            var drag = slotGO.GetComponent<RuneDragItemUI>() ?? slotGO.AddComponent<RuneDragItemUI>();
            drag.Configure(
                runeEntry.Key,
                canDrag: available > 0,
                originKind: RuneDragItemUI.DragOriginKind.Inventory,
                originSlot: SlotIndex.Slot0,
                originModifierIndex: -1,
                quantity: 1);

            var drop = slotGO.GetComponent<RuneDropTargetUI>() ?? slotGO.AddComponent<RuneDropTargetUI>();
            drop.Configure(
                SlotIndex.Slot0,
                RuneDropTargetUI.DropKind.Ability,
                -1,
                onDrop: (dragged) =>
                {
                    if (dragged == null) return;
                    if (dragged.OriginKind == RuneDragItemUI.DragOriginKind.Inventory) return;

                    var panel = GetPanelForSlot(dragged.OriginSlot);
                    panel?.ClearSlotFromOrigin(dragged.OriginKind, dragged.OriginModifierIndex);
                });

            _inventorySlots.Add(slotUI);
        }
    }

    public void SetCraftingPanelTarget(int panelIndex, SlotIndex slot)
    {
        if (panelIndex >= 0 && panelIndex < craftingPanels.Length && craftingPanels[panelIndex] != null)
        {
            craftingPanels[panelIndex].SetTargetSlot(slot);
        }
    }

    public void DismantleSlot(SlotIndex slot)
    {
        if (_spellCrafter == null) return;
        _spellCrafter.Dismantle(slot);

        RefreshInventoryDisplay();
        foreach (var panel in craftingPanels)
        {
            if (panel == null) continue;
            panel.PopulateFromCurrentSlot();
            panel.RefreshDisplay(_spellCrafter);
        }
    }

    public CraftingRecipePanel GetPanelForSlot(SlotIndex slot)
    {
        foreach (var panel in craftingPanels)
        {
            if (panel == null) continue;
            if (panel.TargetSlot == slot) return panel;
        }
        return null;
    }
}
