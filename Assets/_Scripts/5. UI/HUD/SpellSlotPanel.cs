using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Foundation;
using Core;

namespace UI
{
    /// <summary>
    /// One spell column (Slot0 / Slot1 / Slot2).
    /// Owns the current recipe selection for its slot.
    /// Builds exactly 7 RuneTileUI instances on Awake, never destroys them.
    /// All visual updates go through RefreshDisplay().
    /// </summary>
    public sealed class SpellSlotPanel : MonoBehaviour
    {
        public enum SlotType
        {
            Ability,
            Element,
            Modifier
        }
        
        public RectTransform VisualRoot => _visualRoot;

        [Header("Slot index this panel manages")]
        [SerializeField] private SlotIndex _targetSlot;

        [Header("Anchors — size these in the scene to control tile size per rune type")] 
        [SerializeField] private RectTransform _visualRoot;

        [SerializeField] private RectTransform _abilityAnchor;
        [SerializeField] private RectTransform _elementAnchor;
        [SerializeField] private RectTransform[] _modifierAnchors; // length 5

        [SerializeField] private GameObject _runeTilePrefab;

        // ── Runtime selection ───────────────────────────────────────────────
        private AbilityRuneSO _selectedAbility;
        private ElementRuneSO _selectedElement;
        private ModifierRuneSO[] _selectedModifiers = new ModifierRuneSO[SpellRecipe.MODIFIER_SLOTS];

        // ── Tile references — built once in Awake ───────────────────────────
        private RuneTileUI _abilityTile;
        private RuneTileUI _elementTile;
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
            _abilityTile = CreateTile(_abilityAnchor, SlotType.Ability, -1);

            if (_elementAnchor != null)
                _elementTile = CreateTile(_elementAnchor, SlotType.Element, -1);

            for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
            {
                if (i < _modifierAnchors.Length && _modifierAnchors[i] != null)
                {
                    int captured = i;
                    _modifierTiles[i] = CreateTile(_modifierAnchors[i], SlotType.Modifier, captured);
                }
            }
        }

        private RuneTileUI CreateTile(RectTransform anchor, SlotType slotType, int modIndex)
        {
            var go = Instantiate(_runeTilePrefab, anchor);

            // Stretch tile to fill anchor completely.
            // Anchor sizing in the scene controls the visual size per rune type.
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tile = go.GetComponent<RuneTileUI>();
            
            //Only trigger the slot logic if it was a Left Click
            tile.Init((buttonType) =>
            {
                if (buttonType == PointerEventData.InputButton.Left)
                    _owner.OnSlotTileClicked(this, slotType, modIndex);
            });
            
            return tile;
        }

        // ── Population ──────────────────────────────────────────────────────

        /// <summary>
        /// Called on UI open. Reads the current equipped spell from RunState
        /// and loads it into local selection.
        /// </summary>
        public void PopulateFromRunState()
        {
            var current = RunState.GetSlot(_targetSlot) as SpellInstance;
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

        public bool TryAssign(RuneDefinitionSO rune, SlotType slotType, int modIndex, out RuneDefinitionSO replacedRune)
        {
            replacedRune = null; //Default to null if nothing is replaced
            
            switch (slotType)
            {
                case SlotType.Ability:
                    if (rune is AbilityRuneSO ability)
                    {
                        replacedRune = _selectedAbility;
                        _selectedAbility = ability;
                        return true;
                    }

                    return false;

                case SlotType.Element:
                    if (rune is ElementRuneSO element)
                    {
                        replacedRune = _selectedElement;
                        _selectedElement = element;
                        return true;
                    }

                    return false;

                case SlotType.Modifier:
                    if (rune is ModifierRuneSO modifier &&
                        modIndex >= 0 && modIndex < SpellRecipe.MODIFIER_SLOTS)
                    {
                        replacedRune = _selectedModifiers[modIndex];
                        _selectedModifiers[modIndex] = modifier;
                        return true;
                    }

                    return false;
            }

            return false;
        }

        public RuneDefinitionSO ClearSlot(SlotType slotType, int modIndex)
        {
            //Store reference before clearing
            RuneDefinitionSO clearedRune = null;
            
            switch (slotType)
            {
                case SlotType.Ability:
                    clearedRune = _selectedAbility;
                    _selectedAbility = null;
                    break;

                case SlotType.Element:
                    clearedRune = _selectedElement;
                    _selectedElement = null;
                    break;

                case SlotType.Modifier:
                    if (modIndex >= 0 && modIndex < SpellRecipe.MODIFIER_SLOTS)
                    {
                        clearedRune = _selectedModifiers[modIndex];
                        _selectedModifiers[modIndex] = null;
                    }
                    break;
            }

            return clearedRune; //Give the rune back to the caller
        }

        // ── Display ─────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes all 7 tiles from current _selected* state.
        /// No GameObjects created or destroyed.
        /// </summary>
        public void RefreshDisplay(Func<RuneDefinitionSO, bool> shouldHighlight)
        {
            if (_abilityTile != null)
                _abilityTile.Refresh(
                    _selectedAbility,
                    _selectedAbility != null && shouldHighlight(_selectedAbility));

            if (_elementTile != null)
                _elementTile.Refresh(
                    _selectedElement,
                    _selectedElement != null && shouldHighlight(_selectedElement));

            for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
            {
                if (_modifierTiles[i] == null) 
                    continue;
                
                var mod = _selectedModifiers[i];
                _modifierTiles[i].Refresh(
                    mod,
                    mod != null && shouldHighlight(mod));
            }
        }

        // ── Apply on close ──────────────────────────────────────────────────

        public bool TryApply(SpellCrafter crafter)
        {
            if (_selectedAbility == null)
            {
                crafter.Dismantle(_targetSlot);
                return true;
            }

            var recipe = new SpellRecipe(_selectedAbility, _selectedElement, _selectedModifiers);
            return crafter.TryCreate(recipe, _targetSlot, out _);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void ClearAllSelections()
        {
            _selectedAbility = null;
            _selectedElement = null;
            Array.Clear(_selectedModifiers, 0, _selectedModifiers.Length);
        }

        public SlotIndex TargetSlot => _targetSlot;

        public void SetInteractable(bool interactable)
        {
            if (_abilityTile != null)
                _abilityTile.GetComponent<Button>().interactable = interactable;

            if (_elementTile != null)
                _elementTile.GetComponent<Button>().interactable = interactable;

            foreach (var tile in _modifierTiles)
            {
                if (tile != null)
                    tile.GetComponent<Button>().interactable = interactable;
            }
        }
        
        // ── Auto-Assignment ─────────────────────────────────────────────────────────
        /// <summary>
        /// Attempts to assign the rune to the first available empty slot of its type.
        /// Returns true if successfully assigned, false if slots are full.
        /// </summary>
        /// <param name="rune"></param>
        /// <returns></returns>
        public bool TryAutoAssignEmptySlot(RuneDefinitionSO rune)
        {
            if (rune is AbilityRuneSO ability)
            {
                if (_selectedAbility == null)
                {
                    _selectedAbility = ability;
                    return true;
                }
            }
            else if (rune is ElementRuneSO element)
            {
                if (_selectedElement == null)
                {
                    _selectedElement = element;
                    return true;
                }
            }
            else if (rune is ModifierRuneSO modifier)
            {
                //Find the first empty modifier slot
                for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
                {
                    if (_selectedModifiers[i] == null)
                    {
                        _selectedModifiers[i] = modifier;
                        return true;
                    }
                }
            }

            //No empty slot available for this rune type
            return false; 
        }
    }
}
