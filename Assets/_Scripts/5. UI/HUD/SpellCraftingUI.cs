using UnityEngine;
using UnityEngine.UI;
using Foundation;
using System.Collections.Generic;
using Core;

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
    private bool _lastCraftSuccessful = false;

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
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log($"SpellCraftingUI: Toggle key pressed (toggleKey={toggleKey} or C). _uiOpen={_uiOpen}");
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
        _lastCraftSuccessful = false;

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
                panel.RefreshDisplay(_spellCrafter);
                panel.PopulateWithAvailableRunes();
            }
        }

        // Disable close button until craft is successful
        if (closeButton != null)
            closeButton.interactable = false;

        Debug.Log("SpellCraftingUI: OpenCraftingUI completed.");
    }

    private void OnCloseButtonClicked()
    {
        if (!_lastCraftSuccessful)
        {
            Debug.LogWarning("Cannot close crafting UI until a spell is successfully crafted");
            return;
        }

        CloseCraftingUI();
    }

    public void CloseCraftingUI()
    {
        if (!_uiOpen)
            return;

        _uiOpen = false;
        
        // Hide UI
        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        // Resume game
        Time.timeScale = 1f;
    }

    public void NotifyCraftSuccessful()
    {
        _lastCraftSuccessful = true;
        
        if (closeButton != null)
            closeButton.interactable = true;

        // Update remaining rune counts after successful crafting.
        RefreshInventoryDisplay();

        foreach (var panel in craftingPanels)
        {
            if (panel != null)
                panel.PopulateWithAvailableRunes();
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
            if (available <= 0)
                continue;

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
            slotUI.Setup(runeEntry.Key, available, OnInventoryRuneClicked);

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

    private void OnInventoryRuneClicked(RuneDefinitionSO rune)
    {
        if (rune == null)
            return;

        // Try to assign to first available crafting panel
        foreach (var panel in craftingPanels)
        {
            if (panel == null)
                continue;

            if (panel.TryApplyRuneFromInventory(rune))
            {
                Debug.Log($"SpellCraftingUI: Assigned rune {rune.name} to crafting panel {panel.name}");
                // Refresh counts from runestate after assignment
                RefreshInventoryDisplay();
                return;
            }
        }

        Debug.Log("SpellCraftingUI: No available panel slot to place this rune or rune is invalid");
    }
}
