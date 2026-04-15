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

        // One entry per owned rune instance - same rune type appears N times
        // if the player owns N copies.
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
            //Destroy all existing tiles.
            foreach (var entry in _tiles)
            {
                if (entry.tile != null)
                    Destroy(entry.tile.gameObject);
            }
            _tiles.Clear();
            
            //Loop through the inventory definitions.
            foreach (var entry in GameStateManager.RunState.RuneInventory)
            {
                //Apply the filter
                if (currentFilter == RuneFilter.Ability && !(entry.Key is AbilityRuneSO))
                    continue;

                if (currentFilter == RuneFilter.Element && !(entry.Key is ElementRuneSO))
                    continue;

                if (currentFilter == RuneFilter.Cast && !(entry.Key is CastRuneSO))
                    continue;

                if (currentFilter == RuneFilter.OnHit && !(entry.Key is OnHitRuneSO))
                    continue;
                
                int availableCount = availableCountProvider?.Invoke(entry.Key) ?? GameStateManager.RunState.AvailableCount(entry.Key);
                for (int i = 0; i < availableCount; i++)
                {
                    _tiles.Add((entry.Key, BuildTile(entry.Key)));
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
            // Only add the tile if it matches the current filter
            if (MatchesCurrentFilter(rune))
                _tiles.Add((rune, BuildTile(rune)));
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

            tile.Init((buttonType) =>
            {
                int currentIndex = _tiles.FindIndex(t => t.tile == tile);
                _owner.OnInventoryTileClicked(rune, currentIndex, buttonType);
            });
            
            tile.Refresh(rune, false);
            return tile;
        }
    }
}