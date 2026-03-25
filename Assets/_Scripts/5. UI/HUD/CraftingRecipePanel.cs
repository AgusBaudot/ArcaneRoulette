using UnityEngine;
using UnityEngine.UI;
using Foundation;
using Core;

public class CraftingRecipePanel : MonoBehaviour
{
    [SerializeField] private Transform abilityRuneGridParent;
    [SerializeField] private Transform elementRuneGridParent;
    [SerializeField] private Transform modifierRuneGridParent;
    [SerializeField] private Button craftButton;
    [SerializeField] private Text recipeStatusText;
    [SerializeField] private SlotIndex targetSlot = SlotIndex.BasicAttack;

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

            _abilitySlotUI = CreateRuneSlot(abilityRuneGridParent, null, OnAbilitySelected, true);
        }

        // Update element rune slots
        if (elementRuneGridParent != null)
        {
            var existingSlots = elementRuneGridParent.GetComponentsInChildren<RuneSlotUI>();
            foreach (var slot in existingSlots)
                Destroy(slot.gameObject);

            _elementSlotUI = CreateRuneSlot(elementRuneGridParent, null, OnElementSelected, true);
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
                _modifierSlotUIs[i] = CreateRuneSlot(modifierRuneGridParent, null, (rune) => OnModifierSelected(index, rune), true);
            }
        }

        ValidateRecipe();
    }

    private RuneSlotUI CreateRuneSlot(Transform parent, RuneDefinitionSO rune, System.Action<RuneDefinitionSO> onSelected, bool isCraftSlot = false)
    {
        var slotPrefab = Resources.Load<GameObject>("UI/RuneSlot");
        RuneSlotUI runeSlotUI;

        if (slotPrefab == null)
        {
            // Create a basic slot if prefab doesn't exist
            var slot = new GameObject("RuneSlot");
            slot.transform.SetParent(parent);
            runeSlotUI = slot.AddComponent<RuneSlotUI>();
        }
        else
        {
            var instance = Instantiate(slotPrefab, parent);
            runeSlotUI = instance.GetComponent<RuneSlotUI>();
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

        return runeSlotUI;
    }

    public void PopulateWithAvailableRunes()
    {
        // Populate ability rune slots
        if (_abilitySlotUI != null && RunState.RuneInventory.Count > 0)
        {
            foreach (var runeEntry in RunState.RuneInventory)
            {
                if (runeEntry.Key is AbilityRuneSO abilityRune)
                {
                    int available = RunState.AvailableCount(runeEntry.Key);
                    _abilitySlotUI.Setup(abilityRune, available, OnAbilitySelected);
                    break; // For now, show first ability rune
                }
            }
        }

        // Populate element rune slots
        if (_elementSlotUI != null && RunState.RuneInventory.Count > 0)
        {
            foreach (var runeEntry in RunState.RuneInventory)
            {
                if (runeEntry.Key is ElementRuneSO elementRune)
                {
                    int available = RunState.AvailableCount(runeEntry.Key);
                    _elementSlotUI.Setup(elementRune, available, OnElementSelected);
                    break; // For now, show first element rune
                }
            }
        }

        // Populate modifier rune slots
        int modifierIndex = 0;
        if (RunState.RuneInventory.Count > 0)
        {
            foreach (var runeEntry in RunState.RuneInventory)
            {
                if (runeEntry.Key is ModifierRuneSO modifierRune && modifierIndex < SpellRecipe.MODIFIER_SLOTS)
                {
                    int available = RunState.AvailableCount(runeEntry.Key);
                    _modifierSlotUIs[modifierIndex].Setup(modifierRune, available, (r) => OnModifierSelected(modifierIndex, r));
                    modifierIndex++;
                }
            }
        }

        ValidateRecipe();
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

    public void SetTargetSlot(SlotIndex slot)
    {
        targetSlot = slot;
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
