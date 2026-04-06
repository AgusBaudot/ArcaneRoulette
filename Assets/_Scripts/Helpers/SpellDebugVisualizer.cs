// using System;
// using UnityEngine;
// using Foundation;
// using Core;
//
// public sealed class SpellDebugVisualizer : MonoBehaviour
// {
//     [SerializeField] private PlayerController _player;
//
//     private readonly string[] _slotNames = { "BasicAttack", "Dash", "Shield" };
//
//     private void OnGUI()
//     {
//         if (_player == null) return;
//         var state = GameStateManager.RunState;
//         if (state == null) return;
//
//         // ── Panel background ─────────────────────────────────────────────────
//         float panelX = 10f, panelY = 10f, panelW = 420f;
//         float lineH = 22f, pad = 10f;
//
//         //Count lines first so we can size the box
//         int lines = 3; //header + inventory header + separator
//         lines += state.RuneInventory.Count;
//         lines += 2; //separator + slots header
//         lines += _slotNames.Length * 6; //per slot: name, ability, element, modifies, available, separator
//
//         GUI.Box(new Rect(panelX, panelY, panelW, lines * lineH + pad * 2), GUIContent.none);
//
//         float y = panelY + pad;
//
//         // ── Header ───────────────────────────────────────────────────────────
//         GUI.Label(new Rect(panelX + pad, y, panelW, lineH), "<b>SPELL DEBUG VISUALIZER</b>");
//         y += lineH;
//
//         // ── Rune inventory ───────────────────────────────────────────────────
//         GUI.Label(new Rect(panelX + pad, y, panelW, lineH),
//             "── Inventory ────────────────────────");
//         y += lineH;
//
//         foreach (var pair in state.RuneInventory)
//         {
//             state.RuneAllocated.TryGetValue(pair.Key, out int allocated);
//             int available = pair.Value - allocated;
//             string label =
//                 $"  {pair.Key.name,-28} owned: {pair.Value}  alloc: {allocated}  free: {available}";
//
//             // Turn red when fully allocated
//             var prev = GUI.color;
//             GUI.color = available == 0 ? Color.red : Color.white;
//             GUI.Label(new Rect(panelX + pad, y, panelW, lineH), label);
//             GUI.color = prev;
//             y += lineH;
//         }
//
//         // ── Equipped spells ──────────────────────────────────────────────────
//         GUI.Label(new Rect(panelX + pad, y, panelW, lineH),
//             "── Equipped spells ───────────────────");
//         y += lineH;
//
//         for (int i = 0; i < _slotNames.Length; i++)
//         {
//             var slot = state.GetSlot((SlotIndex)i) as SpellInstance;
//
//             GUI.Label(new Rect(panelX + pad, y, panelW, lineH),
//                 $"  [{_slotNames[i]}]");
//             y += lineH;
//
//             if (slot == null)
//             {
//                 GUI.Label(new Rect(panelX + pad * 2, y, panelW, lineH),
//                     "    <empty>");
//                 y += lineH;
//                 continue;
//             }
//
//             var recipe = slot.Recipe;
//
//             // Ability rune
//             GUI.Label(new Rect(panelX + pad * 2, y, panelW, lineH),
//                 $"    Ability:  {recipe.Ability?.name ?? "none"}");
//             y += lineH;
//
//             // Element rune
//             GUI.Label(new Rect(panelX + pad * 2, y, panelW, lineH),
//                 $"    Element:  {(recipe.HasElement ? recipe.Element.name : "none")}");
//             y += lineH;
//
//             // Modifier slots — show each slot entry, including nulls
//             var mods = recipe.Modifiers;
//             for (int m = 0; m < mods.Count; m++)
//             {
//                 string modName = mods[m] != null ? mods[m].name : "—";
//                 GUI.Label(new Rect(panelX + pad * 2, y, panelW, lineH),
//                     $"    Slot {m + 1}:   {modName}");
//                 y += lineH;
//             }
//
//             // Hold vs instant
//             string kind = slot is IHoldAbility ? "Hold" : "Instant";
//             GUI.Label(new Rect(panelX + pad * 2, y, panelW, lineH),
//                 $"    Type:     {kind}  |  Cooldown: {recipe.Ability.CooldownDuration:F2}s");
//             y += lineH;
//
//             y += 4f; // slot separator
//         }
//     }
// }