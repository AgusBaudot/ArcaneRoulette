using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Foundation;
using Core;

public class CraftingRecipePanel : MonoBehaviour
{
    [SerializeField] private Transform abilityRuneGridParent;
    [SerializeField] private Transform elementRuneGridParent;
    [SerializeField] private Transform modifierRuneGridParent;
    [SerializeField] private GameObject runeSlotPrefab;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI recipeStatusText;
    [SerializeField] private SlotIndex targetSlot = SlotIndex.Slot0;

    private RuneSlotUI _abilitySlotUI;
    private RuneSlotUI _elementSlotUI;
    private RuneSlotUI[] _modifierSlotUIs = new RuneSlotUI[SpellRecipe.MODIFIER_SLOTS];

    private AbilityRuneSO _selectedAbility;
    private ElementRuneSO _selectedElement;
    private ModifierRuneSO[] _selectedModifiers = new ModifierRuneSO[SpellRecipe.MODIFIER_SLOTS];

    private VolatileRunState RunState => GameStateManager.RunState;
    private SpellCrafter _spellCrafter;
    private SpellCraftingUI _craftingUI;

    private void Awake()
    {
        _spellCrafter = FindObjectOfType<SpellCrafter>();
        _craftingUI = FindObjectOfType<SpellCraftingUI>();
        
        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    public void RefreshDisplay(SpellCrafter crafter)
    {
        _spellCrafter = crafter;
        
        // Update ability rune slots
        if (abilityRuneGridParent != null)
        {
            var existingSlots = abilityRuneGridParent.GetComponentsInChildren<RuneSlotUI>();
            foreach (var slot in existingSlots)
                Destroy(slot.gameObject);

            _abilitySlotUI = CreateRuneSlot(
                ResolveAbilityAnchor(),
                _selectedAbility,
                OnAbilitySelected,
                isCraftSlot: true,
                dropKind: RuneDropTargetUI.DropKind.Ability,
                modifierIndex: -1);
        }

        // Update element rune slots
        if (elementRuneGridParent != null)
        {
            var existingSlots = elementRuneGridParent.GetComponentsInChildren<RuneSlotUI>();
            foreach (var slot in existingSlots)
                Destroy(slot.gameObject);

            _elementSlotUI = CreateRuneSlot(
                ResolveElementAnchor(),
                _selectedElement,
                OnElementSelected,
                isCraftSlot: true,
                dropKind: RuneDropTargetUI.DropKind.Element,
                modifierIndex: -1);
        }

        // Update modifier rune slots
        if (modifierRuneGridParent != null)
        {
            var existingSlots = modifierRuneGridParent.GetComponentsInChildren<RuneSlotUI>();
            foreach (var slot in existingSlots)
                Destroy(slot.gameObject);

            for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
            {
                int index = i; // Capture for closure
                _modifierSlotUIs[i] = CreateRuneSlot(
                    ResolveModifierAnchor(index),
                    _selectedModifiers[index],
                    (rune) => OnModifierSelected(index, rune),
                    isCraftSlot: true,
                    dropKind: RuneDropTargetUI.DropKind.Modifier,
                    modifierIndex: index);
            }
        }

        ValidateRecipe();
    }

    private RuneSlotUI CreateRuneSlot(
        Transform parent,
        RuneDefinitionSO rune,
        System.Action<RuneDefinitionSO> onSelected,
        bool isCraftSlot = false,
        RuneDropTargetUI.DropKind? dropKind = null,
        int modifierIndex = -1)
    {
        GameObject slotPrefab = this.runeSlotPrefab;
        if (slotPrefab == null)
            slotPrefab = Resources.Load<GameObject>("RuneSlot");

        RuneSlotUI runeSlotUI;
        GameObject slot;

        if (slotPrefab != null)
        {
            slot = Instantiate(slotPrefab, parent);
            runeSlotUI = slot.GetComponent<RuneSlotUI>();
            if (runeSlotUI == null)
                runeSlotUI = slot.AddComponent<RuneSlotUI>();
        }
        else
        {
            slot = new GameObject("RuneSlot");
            slot.transform.SetParent(parent);
            runeSlotUI = slot.AddComponent<RuneSlotUI>();
        }

        if (isCraftSlot)
        {
            runeSlotUI.SetAsCraftingSlot(null);
            runeSlotUI.Setup(rune, rune != null ? RunState.AvailableCount(rune) : 0, null);
        }
        else
        {
            runeSlotUI.Setup(rune, rune != null ? RunState.AvailableCount(rune) : 0, onSelected);
        }

        if (isCraftSlot)
        {
            // Drag support (slot -> inventory to remove; inventory -> slot to add).
            var drag = slot.GetComponent<RuneDragItemUI>() ?? slot.AddComponent<RuneDragItemUI>();
            drag.Configure(
                rune,
                canDrag: rune != null,
                originKind: dropKind switch
                {
                    RuneDropTargetUI.DropKind.Ability => RuneDragItemUI.DragOriginKind.SlotAbility,
                    RuneDropTargetUI.DropKind.Element => RuneDragItemUI.DragOriginKind.SlotElement,
                    _ => RuneDragItemUI.DragOriginKind.SlotModifier
                },
                originSlot: targetSlot,
                originModifierIndex: modifierIndex,
                quantity: 1);

            // Drop support (inventory -> this slot).
            if (dropKind.HasValue)
            {
                var drop = slot.GetComponent<RuneDropTargetUI>() ?? slot.AddComponent<RuneDropTargetUI>();
                drop.Configure(
                    targetSlot,
                    dropKind.Value,
                    modifierIndex,
                    onDrop: (dragged) =>
                    {
                        if (dragged == null || dragged.Rune == null) return;
                        var sourceRune = dragged.Rune;

                        // Type safety: reject incompatible rune types.
                        bool isCompatible = dropKind.Value switch
                        {
                            RuneDropTargetUI.DropKind.Ability => dragged.Rune is AbilityRuneSO,
                            RuneDropTargetUI.DropKind.Element => dragged.Rune is ElementRuneSO,
                            _ => dragged.Rune is ModifierRuneSO
                        };

                        if (!isCompatible)
                            return;

                        // Slot -> Slot move: support swap if destination is occupied.
                        if (dragged.OriginKind != RuneDragItemUI.DragOriginKind.Inventory)
                        {
                            var ui = _craftingUI != null ? _craftingUI : FindObjectOfType<SpellCraftingUI>();
                            var originPanel = ui != null ? ui.GetPanelForSlot(dragged.OriginSlot) : null;
                            if (originPanel == null) return;

                            // Enforce "same type" moves/swaps.
                            bool sameKind = dropKind.Value switch
                            {
                                RuneDropTargetUI.DropKind.Ability => dragged.OriginKind == RuneDragItemUI.DragOriginKind.SlotAbility,
                                RuneDropTargetUI.DropKind.Element => dragged.OriginKind == RuneDragItemUI.DragOriginKind.SlotElement,
                                _ => dragged.OriginKind == RuneDragItemUI.DragOriginKind.SlotModifier
                            };

                            if (!sameKind)
                                return;

                            // Capture destination rune (if any) to place back into origin (swap).
                            RuneDefinitionSO destRune = dropKind.Value switch
                            {
                                RuneDropTargetUI.DropKind.Ability => _selectedAbility,
                                RuneDropTargetUI.DropKind.Element => _selectedElement,
                                _ => modifierIndex >= 0 && modifierIndex < SpellRecipe.MODIFIER_SLOTS
                                    ? _selectedModifiers[modifierIndex]
                                    : null
                            };

                            if (dragged.OriginKind == RuneDragItemUI.DragOriginKind.SlotAbility)
                                originPanel.SetAbility(destRune as AbilityRuneSO);
                            else if (dragged.OriginKind == RuneDragItemUI.DragOriginKind.SlotElement)
                                originPanel.SetElement(destRune as ElementRuneSO);
                            else
                                originPanel.SetModifier(dragged.OriginModifierIndex, destRune as ModifierRuneSO);
                        }

                        // Assign into this slot.
                        if (dropKind.Value == RuneDropTargetUI.DropKind.Ability)
                            SetAbility((AbilityRuneSO)sourceRune);
                        else if (dropKind.Value == RuneDropTargetUI.DropKind.Element)
                            SetElement((ElementRuneSO)sourceRune);
                        else
                            SetModifier(modifierIndex, (ModifierRuneSO)sourceRune);
                    });
            }
        }

        return runeSlotUI;
    }

    private Transform ResolveAbilityAnchor()
    {
        if (abilityRuneGridParent == null) return null;
        // If parent has no children, treat parent itself as the anchor.
        if (abilityRuneGridParent.childCount == 0) return abilityRuneGridParent;

        for (int i = 0; i < abilityRuneGridParent.childCount; i++)
        {
            var c = abilityRuneGridParent.GetChild(i);
            if (c.name.Contains("Ability"))
                return c;
        }

        return abilityRuneGridParent.GetChild(0);
    }

    private Transform ResolveElementAnchor()
    {
        if (elementRuneGridParent == null) return null;
        if (elementRuneGridParent.childCount == 0) return elementRuneGridParent;

        for (int i = 0; i < elementRuneGridParent.childCount; i++)
        {
            var c = elementRuneGridParent.GetChild(i);
            if (c.name.Contains("Element"))
                return c;
        }

        return elementRuneGridParent.GetChild(0);
    }

    private Transform ResolveModifierAnchor(int modifierIndex)
    {
        if (modifierRuneGridParent == null) return null;
        if (modifierRuneGridParent.childCount == 0) return modifierRuneGridParent;

        // Preferred: explicit named anchors like "... Anchor 1" to "... Anchor 5"
        string suffix = (modifierIndex + 1).ToString();
        for (int i = 0; i < modifierRuneGridParent.childCount; i++)
        {
            var c = modifierRuneGridParent.GetChild(i);
            if (c.name.Contains("Modifier") && c.name.EndsWith(suffix))
                return c;
        }

        // Fallback: child index mapping.
        if (modifierIndex >= 0 && modifierIndex < modifierRuneGridParent.childCount)
            return modifierRuneGridParent.GetChild(modifierIndex);

        return modifierRuneGridParent;
    }

    // For now we show the current spell that exists in this panel's SlotIndex.
    // Later Dev1 will own how this UI validates/allocates runes.
    public void PopulateFromCurrentSlot()
    {
        var current = RunState.GetSlot(targetSlot) as SpellInstance;
        if (current == null)
        {
            ClearSelection();
            return;
        }

        _selectedAbility = current.Recipe.Ability;
        _selectedElement = current.Recipe.Element;

        var mods = current.Recipe.Modifiers;
        for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
            _selectedModifiers[i] = i < mods.Count ? mods[i] : null;

        // RefreshDisplay() will rebuild slot visuals using current _selected* vars.
    }

    private void OnAbilitySelected(RuneDefinitionSO rune)
    {
        _selectedAbility = rune as AbilityRuneSO;
        ValidateRecipe();
    }

    private void OnElementSelected(RuneDefinitionSO rune)
    {
        _selectedElement = rune as ElementRuneSO;
        ValidateRecipe();
    }

    private void OnModifierSelected(int index, RuneDefinitionSO rune)
    {
        if (index < SpellRecipe.MODIFIER_SLOTS)
            _selectedModifiers[index] = rune as ModifierRuneSO;
        ValidateRecipe();
    }

    public AbilityRuneSO GetAbility() => _selectedAbility;
    public ElementRuneSO GetElement() => _selectedElement;
    public ModifierRuneSO GetModifier(int index)
        => index >= 0 && index < SpellRecipe.MODIFIER_SLOTS ? _selectedModifiers[index] : null;

    // Called by drag/drop.
    public void SetAbility(AbilityRuneSO ability)
    {
        _selectedAbility = ability;
        ValidateRecipe();
        RefreshSlotVisuals();
    }

    public void SetElement(ElementRuneSO element)
    {
        _selectedElement = element;
        ValidateRecipe();
        RefreshSlotVisuals();
    }

    public void SetModifier(int index, ModifierRuneSO modifier)
    {
        if (index < 0 || index >= SpellRecipe.MODIFIER_SLOTS) return;
        _selectedModifiers[index] = modifier;
        ValidateRecipe();
        RefreshSlotVisuals();
    }

    public void ClearSlotFromOrigin(RuneDragItemUI.DragOriginKind originKind, int modifierIndex)
    {
        switch (originKind)
        {
            case RuneDragItemUI.DragOriginKind.SlotAbility:
                _selectedAbility = null;
                break;
            case RuneDragItemUI.DragOriginKind.SlotElement:
                _selectedElement = null;
                break;
            case RuneDragItemUI.DragOriginKind.SlotModifier:
                if (modifierIndex >= 0 && modifierIndex < SpellRecipe.MODIFIER_SLOTS)
                    _selectedModifiers[modifierIndex] = null;
                break;
        }

        ValidateRecipe();
        RefreshSlotVisuals();
    }

    private void RefreshSlotVisuals()
    {
        if (_abilitySlotUI != null)
        {
            _abilitySlotUI.Setup(_selectedAbility, _selectedAbility != null ? RunState.AvailableCount(_selectedAbility) : 0, null);
            var drag = _abilitySlotUI.GetComponent<RuneDragItemUI>();
            if (drag != null)
                drag.Configure(_selectedAbility, _selectedAbility != null, RuneDragItemUI.DragOriginKind.SlotAbility, targetSlot, -1, 1);
        }
        if (_elementSlotUI != null)
        {
            _elementSlotUI.Setup(_selectedElement, _selectedElement != null ? RunState.AvailableCount(_selectedElement) : 0, null);
            var drag = _elementSlotUI.GetComponent<RuneDragItemUI>();
            if (drag != null)
                drag.Configure(_selectedElement, _selectedElement != null, RuneDragItemUI.DragOriginKind.SlotElement, targetSlot, -1, 1);
        }
        if (_modifierSlotUIs != null)
        {
            for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
            {
                var ui = _modifierSlotUIs[i];
                if (ui == null) continue;
                var r = _selectedModifiers[i];
                ui.Setup(r, r != null ? RunState.AvailableCount(r) : 0, null);
                var drag = ui.GetComponent<RuneDragItemUI>();
                if (drag != null)
                    drag.Configure(r, r != null, RuneDragItemUI.DragOriginKind.SlotModifier, targetSlot, i, 1);
            }
        }
    }

    public bool TryApplyRuneFromInventory(RuneDefinitionSO rune)
    {
        // apply ability first (required)
        if (rune is AbilityRuneSO abilityRune)
        {
            if (_selectedAbility == null)
            {
                _selectedAbility = abilityRune;
                if (_abilitySlotUI != null)
                    _abilitySlotUI.Setup(abilityRune, RunState.AvailableCount(abilityRune), OnAbilitySelected);
                ValidateRecipe();
                return true;
            }
            return false;
        }

        // apply element if ability is set
        if (rune is ElementRuneSO elementRune)
        {
            if (_selectedElement == null)
            {
                _selectedElement = elementRune;
                if (_elementSlotUI != null)
                    _elementSlotUI.Setup(elementRune, RunState.AvailableCount(elementRune), OnElementSelected);
                ValidateRecipe();
                return true;
            }
            return false;
        }

        // apply modifier cycle
        if (rune is ModifierRuneSO modifierRune)
        {
            for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
            {
                if (_selectedModifiers[i] == null)
                {
                    _selectedModifiers[i] = modifierRune;
                    if (_modifierSlotUIs[i] != null)
                        _modifierSlotUIs[i].Setup(modifierRune, RunState.AvailableCount(modifierRune), (r) => OnModifierSelected(i, r));
                    ValidateRecipe();
                    return true;
                }
            }
            return false;
        }

        return false;
    }


    private void ValidateRecipe()
    {
        bool isValid = _selectedAbility != null; // Only Ability is required
        
        if (craftButton != null)
            craftButton.interactable = isValid;
        
        if (recipeStatusText != null)
        {
            if (!isValid)
            {
                recipeStatusText.text = "Missing: Ability Rune";
            }
            else
            {
                recipeStatusText.text = "Recipe Complete";
            }
        }
    }

    private void OnCraftButtonClicked()
    {
        if (_selectedAbility == null)
        {
            Debug.LogWarning("CraftingRecipePanel: Cannot craft - missing ability rune");
            return;
        }

        var recipe = new SpellRecipe(_selectedAbility, _selectedElement, _selectedModifiers);
        
        if (_spellCrafter != null && _spellCrafter.TryCreate(recipe, targetSlot, out SpellInstance result))
        {
            Debug.Log($"Spell crafted successfully in slot {targetSlot}");
            ClearSelection();
            ValidateRecipe();
            
            // Notify SpellCraftingUI of successful craft
            if (_craftingUI != null)
                _craftingUI.NotifyCraftSuccessful();
        }
        else
        {
            Debug.LogWarning("CraftingRecipePanel: Crafting failed");
        }
    }

    // Apply current UI selection into VolatileRunState on panel close.
    public bool TryApplySelectionToRunState(SpellCrafter crafter)
    {
        if (crafter == null) return false;

        // Ability is required to have a spell.
        if (_selectedAbility == null)
        {
            crafter.Dismantle(targetSlot);
            return true;
        }

        var recipe = new SpellRecipe(_selectedAbility, _selectedElement, _selectedModifiers);
        return crafter.TryCreate(recipe, targetSlot, out _);
    }

    public void SetTargetSlot(SlotIndex slot)
    {
        targetSlot = slot;
    }

    public SlotIndex TargetSlot => targetSlot;

    public void SetRuneSlotPrefab(GameObject prefab)
    {
        runeSlotPrefab = prefab;
    }

    private void ClearSelection()
    {
        _selectedAbility = null;
        _selectedElement = null;
        System.Array.Clear(_selectedModifiers, 0, _selectedModifiers.Length);

        if (_abilitySlotUI != null) _abilitySlotUI.SetSelected(false);
        if (_elementSlotUI != null) _elementSlotUI.SetSelected(false);
        foreach (var modSlot in _modifierSlotUIs)
        {
            if (modSlot != null) modSlot.SetSelected(false);
        }
    }

    public bool IsRecipeValid => _selectedAbility != null && _selectedElement != null;
}
