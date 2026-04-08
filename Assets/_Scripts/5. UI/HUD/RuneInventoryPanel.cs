using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        public void Init(SpellCraftingUI owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Called on UI open. Rebuilds tiles from scratch to match RunState.
        /// On open only - not after every assign (RemoveTile handles that).
        /// </summary>
        public void Rebuild()
        {
            //Destroy all existing tiles.
            foreach (var entry in _tiles)
            {
                if (entry.tile != null)
                    Destroy(entry.tile.gameObject);
            }
            _tiles.Clear();
            
            //One tile per rune instance in inventory.
            foreach (var entry in GameStateManager.RunState.RuneInventory)
            {
                for (int i = 0; i < entry.Value; i++)
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
            => _tiles.Add((rune, BuildTile(rune)));

        private void RefreshHighlights(Func<int, bool> shouldHighlight)
        {
            Refresh(shouldHighlight ?? (_ => false));
        }

        private RuneTileUI BuildTile(RuneDefinitionSO rune)
        {
            var go = Instantiate(_runeTilePrefab, _gridParent);
            var tile = go.GetComponent<RuneTileUI>();

            tile.Init(() =>
            {
                int currentIndex = _tiles.FindIndex(t => t.tile == tile);
                _owner.OnInventoryTileClicked(rune,currentIndex);
            });
            
            tile.Refresh(rune, false);
            return tile;
        }
    }
}