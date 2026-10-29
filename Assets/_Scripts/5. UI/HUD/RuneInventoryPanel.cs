using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Foundation;

namespace UI
{
    /// <summary>
    /// Left-side inventory list. Builds one RuneTileUI per unique rune type
    /// on first open, refreshes counts and highlights on subsequent opens.
    /// Never destroys tiles after initial build.
    /// </summary>
    public sealed class RuneInventoryPanel : MonoBehaviour
    {
        [SerializeField] private Transform _gridParent;
        [SerializeField] private GameObject _runeTilePrefab;

        private readonly List<(RuneDefinitionSO rune, RuneTileUI tile)> _tiles = new();
        private SpellCraftingUI _owner;
        private RuneFilter _currentFilter;

        public void Init(SpellCraftingUI owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Rebuilds tiles from scratch, filtering by the selected RuneFilter.
        /// </summary>
        public void Rebuild(RuneFilter currentFilter, Func<RuneDefinitionSO, int> availableCountProvider = null)
        {
            _currentFilter = currentFilter;
            foreach (var entry in _tiles)
            {
                if (entry.tile != null)
                    Destroy(entry.tile.gameObject);
            }
            _tiles.Clear();
            
            var sortedRunes = new List<RuneDefinitionSO>();
            foreach (var entry in GameStateManager.RunState.RuneInventory)
            {
                if (MatchesCurrentFilter(entry.Key))
                    sortedRunes.Add(entry.Key);
            }

            sortedRunes.Sort((a, b) => 
            {
                int orderA = GetSortOrder(a);
                int orderB = GetSortOrder(b);
                if (orderA != orderB) return orderA.CompareTo(orderB);
                
                int nameCompare = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                return nameCompare != 0 ? nameCompare : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });

            // 3. Build in precise order
            foreach (var rune in sortedRunes)
            {
                int availableCount = availableCountProvider?.Invoke(rune) ?? GameStateManager.RunState.AvailableCount(rune);
                for (int i = 0; i < availableCount; i++)
                {
                    _tiles.Add((rune, BuildTile(rune)));
                }
            }

            RefreshHighlights(null);
        }

        /// <summary>
        /// Updates highlight state on all tiles. Called by SpellCraftingUI.RefreshAll()
        /// Does not rebuild - tiles are stable between assigns.
        /// </summary>
        public void Refresh(Func<int, bool> shouldHighlight)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i].tile == null)
                    return;
                
                _tiles[i].tile.Refresh(_tiles[i].rune, shouldHighlight(i));

                _tiles[i].tile.GetComponent<Button>().interactable = true;
            }
        }

        /// <summary>
        /// Removes exactly one tile for the given rune type.
        /// Called by SpellCraftingUI after a successful assign.
        /// GridLayoutGroup reflowing handles the visual gap automatically.
        /// </summary>
        /// <param name="rune"></param>
        public void RemoveOneTile(RuneDefinitionSO rune)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i].rune == rune)
                {
                    Destroy(_tiles[i].tile.gameObject);
                    _tiles.RemoveAt(i);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Adds exactly one tile for the given rune type.
        /// Called when a rune is cleared from a slot.
        /// </summary>
        /// <param name="rune"></param>
        public void AddOneTile(RuneDefinitionSO rune)
        {
            if (!MatchesCurrentFilter(rune))
                return;

            int insertIndex = 0;
            int targetSortOrder = GetSortOrder(rune);

            while (insertIndex < _tiles.Count)
            {
                var existingRune = _tiles[insertIndex].rune;
                int existingSortOrder = GetSortOrder(existingRune);

                if (existingSortOrder > targetSortOrder)
                    break;
                
                if (existingSortOrder == targetSortOrder)
                {
                    int nameCompare = string.Compare(existingRune.Name, rune.Name, StringComparison.Ordinal);
                    if (nameCompare > 0)
                        break;
                        
                    if (existingRune == rune) 
                    {
                        while (insertIndex < _tiles.Count && _tiles[insertIndex].rune == rune)
                            insertIndex++;
                        break;
                    }
                }
                insertIndex++;
            }

            var newTile = BuildTile(rune);
            
            newTile.transform.SetSiblingIndex(insertIndex);
            
            _tiles.Insert(insertIndex, (rune, newTile));
        }

        /// <summary>
        /// Forces an immediate layout rebuild and returns the exact screen rect of the requested rune tile.
        /// </summary>
        /// <param name="rune"></param>
        /// <returns></returns>
        public RectTransform GetTileRect(RuneDefinitionSO rune)
        {
            if (_gridParent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_gridParent.GetComponent<RectTransform>());
            }

            foreach (var entry in _tiles)
            {
                if (entry.rune == rune && entry.tile != null)
                    return entry.tile.GetComponent<RectTransform>();
            }
            return null;
        }

        private bool MatchesCurrentFilter(RuneDefinitionSO rune)
        {
            if (_currentFilter == RuneFilter.All)
                return true;

            if (_currentFilter == RuneFilter.Ability && rune is AbilityRuneSO)
                return true;

            if (_currentFilter == RuneFilter.Element && rune is ElementRuneSO)
                return true;

            if (_currentFilter == RuneFilter.Cast && rune is CastRuneSO)
                return true;

            if (_currentFilter == RuneFilter.OnHit && rune is OnHitRuneSO)
                return true;

            return false;
        }

        private void RefreshHighlights(Func<int, bool> shouldHighlight)
        {
            Refresh(shouldHighlight ?? (_ => false));
        }

        private RuneTileUI BuildTile(RuneDefinitionSO rune)
        {
            var go = Instantiate(_runeTilePrefab, _gridParent);
            var tile = go.GetComponent<RuneTileUI>();
            var rectTransform = go.GetComponent<RectTransform>();

            tile.Init((buttonType) =>
            {
                int currentIndex = _tiles.FindIndex(t => t.tile == tile);
                _owner.OnInventoryTileClicked(rune, currentIndex, buttonType, rectTransform);
            });
            
            tile.Refresh(rune, false);
            return tile;
        }
        
        private int GetSortOrder(RuneDefinitionSO rune)
        {
            if (rune is AbilityRuneSO) return 0;
            if (rune is ElementRuneSO) return 1;
            if (rune is CastRuneSO) return 2;
            if (rune is OnHitRuneSO) return 3;
            return 4;
        }
    }
}