using UnityEngine;
using Foundation;
using Core;
using UI;

/// <summary>
/// One spell column (Slot0 / Slot1 / Slot2).
/// Owns the current recipe selection for its slot.
/// Builds exactly 7 RuneTileUI instances on Awake, never destroys them.
/// All visual updates go through RefreshDisplay().
/// </summary>
public sealed class SpellSlotPanel : MonoBehaviour
{
    public enum SlotType { Ability, Element, Modifier }

    [Header("Slot index this panel manages")]
    [SerializeField] private SlotIndex targetSlot;

    [Header("Anchors — size these in the scene to control tile size per rune type")]
    [SerializeField] private RectTransform abilityAnchor;
    [SerializeField] private RectTransform elementAnchor;
    [SerializeField] private RectTransform[] modifierAnchors; // length 5

    [SerializeField] private GameObject runeTilePrefab;

    // ── Runtime selection ───────────────────────────────────────────────
    private AbilityRuneSO   _selectedAbility;
    private ElementRuneSO   _selectedElement;
    private ModifierRuneSO[] _selectedModifiers = new ModifierRuneSO[SpellRecipe.MODIFIER_SLOTS];

    // ── Tile references — built once in Awake ───────────────────────────
    private RuneTileUI   _abilityTile;
    private RuneTileUI   _elementTile;
    private RuneTileUI[] _modifierTiles = new RuneTileUI[SpellRecipe.MODIFIER_SLOTS];

    private SpellCraftingUI _owner;
    private VolatileRunState RunState => GameStateManager.RunState;

    // ── Init ────────────────────────────────────────────────────────────

    public void Init(SpellCraftingUI owner)
    {
        _owner = owner;
        BuildTiles();
    }

    private void BuildTiles()
    {
        _abilityTile = CreateTile(abilityAnchor, SlotType.Ability, -1);

        if (elementAnchor != null)
            _elementTile = CreateTile(elementAnchor, SlotType.Element, -1);

        for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
        {
            if (i < modifierAnchors.Length && modifierAnchors[i] != null)
            {
                int captured = i;
                _modifierTiles[i] = CreateTile(modifierAnchors[i], SlotType.Modifier, captured);
            }
        }
    }

    private RuneTileUI CreateTile(RectTransform anchor, SlotType slotType, int modIndex)
    {
        var go = Instantiate(runeTilePrefab, anchor);

        // Stretch tile to fill anchor completely.
        // Anchor sizing in the scene controls the visual size per rune type.
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = Vector2.zero;
        rect.anchorMax        = Vector2.one;
        rect.offsetMin        = Vector2.zero;
        rect.offsetMax        = Vector2.zero;

        var tile = go.GetComponent<RuneTileUI>();
        tile.Init(() => _owner.OnSlotTileClicked(this, slotType, modIndex));
        return tile;
    }

    // ── Population ──────────────────────────────────────────────────────

    /// <summary>
    /// Called on UI open. Reads the current equipped spell from RunState
    /// and loads it into local selection.
    /// </summary>
    public void PopulateFromRunState()
    {
        var current = RunState.GetSlot(targetSlot) as SpellInstance;
        if (current == null)
        {
            ClearAllSelections();
            return;
        }

        _selectedAbility = current.Recipe.Ability;
        _selectedElement = current.Recipe.Element;

        var mods = current.Recipe.Modifiers;
        for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
            _selectedModifiers[i] = i < mods.Count ? mods[i] : null;
    }

    // ── Assignment (called by SpellCraftingUI) ──────────────────────────

    public bool TryAssign(RuneDefinitionSO rune, SlotType slotType, int modIndex)
    {
        switch (slotType)
        {
            case SlotType.Ability:
                if (rune is AbilityRuneSO ability)
                {
                    _selectedAbility = ability;
                    return true;
                }
                return false;

            case SlotType.Element:
                if (rune is ElementRuneSO element)
                {
                    _selectedElement = element;
                    return true;
                }
                return false;

            case SlotType.Modifier:
                if (rune is ModifierRuneSO modifier &&
                    modIndex >= 0 && modIndex < SpellRecipe.MODIFIER_SLOTS)
                {
                    _selectedModifiers[modIndex] = modifier;
                    return true;
                }
                return false;
        }
        return false;
    }

    public void ClearSlot(SlotType slotType, int modIndex)
    {
        switch (slotType)
        {
            case SlotType.Ability:   _selectedAbility = null; break;
            case SlotType.Element:   _selectedElement = null; break;
            case SlotType.Modifier:
                if (modIndex >= 0 && modIndex < SpellRecipe.MODIFIER_SLOTS)
                    _selectedModifiers[modIndex] = null;
                break;
        }
    }

    // ── Display ─────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes all 7 tiles from current _selected* state.
    /// No GameObjects created or destroyed.
    /// </summary>
    public void RefreshDisplay(RuneDefinitionSO pendingRune)
    {
        if (_abilityTile != null)
            _abilityTile.Refresh(
                _selectedAbility,
                _selectedAbility != null ? RunState.AvailableCount(_selectedAbility) : 0,
                _selectedAbility != null && _selectedAbility == pendingRune);

        if (_elementTile != null)
            _elementTile.Refresh(
                _selectedElement,
                _selectedElement != null ? RunState.AvailableCount(_selectedElement) : 0,
                _selectedElement != null && _selectedElement == pendingRune);

        for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
        {
            if (_modifierTiles[i] == null) continue;
            var mod = _selectedModifiers[i];
            _modifierTiles[i].Refresh(
                mod,
                mod != null ? RunState.AvailableCount(mod) : 0,
                mod != null && mod == pendingRune);
        }
    }

    // ── Apply on close ──────────────────────────────────────────────────

    public bool TryApply(SpellCrafter crafter)
    {
        if (_selectedAbility == null)
        {
            crafter.Dismantle(targetSlot);
            return true;
        }

        var recipe = new SpellRecipe(_selectedAbility, _selectedElement, _selectedModifiers);
        return crafter.TryCreate(recipe, targetSlot, out _);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void ClearAllSelections()
    {
        _selectedAbility = null;
        _selectedElement = null;
        System.Array.Clear(_selectedModifiers, 0, _selectedModifiers.Length);
    }

    public SlotIndex TargetSlot => targetSlot;
}