using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;
using UI;

/// <summary>
/// Left-side inventory list. Builds one RuneTileUI per unique rune type
/// on first open, refreshes counts and highlights on subsequent opens.
/// Never destroys tiles after initial build.
/// </summary>
public sealed class RuneInventoryPanel : MonoBehaviour
{
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject runeTilePrefab;

    // rune → tile, built once, reused forever.
    private readonly Dictionary<RuneDefinitionSO, RuneTileUI> _tiles = new();
    private SpellCraftingUI _owner;

    public void Init(SpellCraftingUI owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Call on UI open and after every assign. Builds missing tiles,
    /// refreshes counts and highlights on all existing ones.
    /// </summary>
    public void Refresh(Func<RuneDefinitionSO, bool> shouldHighlight)
    {
        var inventory = GameStateManager.RunState.RuneInventory;
        
        foreach (var entry in inventory)
        {
            var rune = entry.Key;
        
            if (!_tiles.TryGetValue(rune, out var tile))
                tile = BuildTile(rune);
        
            int available = GameStateManager.RunState.AvailableCount(rune);
            tile.Refresh(rune, available, shouldHighlight(rune));
        
            // Tiles with zero available are shown but not interactable
            // unless they are already the pending selection.
            tile.GetComponent<UnityEngine.UI.Button>().interactable =
                available > 0 || shouldHighlight(rune);
        }
        
        foreach (var entry in _tiles)
        {
            entry.Value.Refresh(entry.Key, GameStateManager.RunState.AvailableCount(entry.Key), shouldHighlight(entry.Key));
        }
    }

    private RuneTileUI BuildTile(RuneDefinitionSO rune)
    {
        var go = Instantiate(runeTilePrefab, gridParent);
        var tile = go.GetComponent<RuneTileUI>();

        tile.Init(() => _owner.OnInventoryTileClicked(rune));
        _tiles[rune] = tile;
        return tile;
    }
}