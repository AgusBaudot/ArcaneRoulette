# Spell Crafting System UI - Integration Guide

## Quick Start Summary

**What you need to do:**
1. Add `SpellCraftingUI` component to **Canvas**
2. Create **Close Button** (UI → Button) and assign it
3. Add `CraftingRecipePanel` to each of the **3 spell containers**
4. Create **Craft Button** and **Status Text** for each panel
5. Verify scene has required components
6. Test by pressing **Tab** in Play mode

**Time estimate:** 10-15 minutes

## Final Hierarchy Preview

After setup, your Canvas hierarchy should look like this:

```
Canvas (SpellCraftingUI component)
├── Background
├── CloseButton (Button) ← Created in Step 2
├── Main Inventory Side
│   └── Inventory Grid (populated by script)
└── Crafting Side
    ├── First Spell (CraftingRecipePanel component)
    │   ├── Ability Rune Left Grid
    │   ├── Element Left Craft Grid
    │   ├── Effect/Impact Left Craft Grid (5 slots)
    │   ├── Craft Result Left Grid
    │   ├── CraftButton (Button) ← Created in Step 3
    │   └── StatusText (Text) ← Created in Step 3
    ├── Second Spell (CraftingRecipePanel component)
    │   ├── [same structure as First Spell]
    │   ├── CraftButton (Button)
    │   └── StatusText (Text)
    └── Third Spell (CraftingRecipePanel component)
        ├── [same structure as First Spell]
        ├── CraftButton (Button)
        └── StatusText (Text)
```

## Overview

This guide helps you integrate the Spell Crafting UI system into your **Dev3Testing** scene. The system provides a complete UI for crafting spells using runes.

## System Architecture

### Three New UI Scripts

1. **RuneSlotUI.cs** - Represents a single rune slot in the UI
   - Displays rune icon and available count
   - Handles selection state
   - Provides click feedback

2. **CraftingRecipePanel.cs** - Manages one spell crafting slot
   - Shows Ability, Element, and Modifier rune slots
   - Validates recipe (Ability Rune required)
   - Calls SpellCrafter.TryCreate on craft button click
   - Notifies SpellCraftingUI on successful craft

3. **SpellCraftingUI.cs** - Main controller for entire crafting system
   - Opens/closes crafting UI with Tab key
   - Pauses game (Time.timeScale = 0)
   - Displays inventory of available runes
   - Manages multiple CraftingRecipePanels
   - Prevents closing until successful craft

## Crafting Rules Implemented

✓ **Recipe Validation**: Requires an Ability Rune (Element is optional)
✓ **Game Pausing**: Sets Time.timeScale to 0 when UI opens
✓ **Forced Crafting**: Close button disabled until SpellRecipe.TryCreate succeeds
✓ **VolatileRunState Integration**: Uses only:
  - `RunState.RuneInventory` - Available runes inventory
  - `RunState.RuneAllocated` - Currently allocated runes
  - `RunState.AvailableCount(rune)` - Calculate available count
  - `RunState.GetSlot(slotIndex)` - Read current spell in slot

## Integration Steps

### Step 1: Add Components to Canvas

Your Dev3Testing scene already has the correct structure:
```
Canvas
├── Background
├── Main Inventory Side
│   └── Inventory Grid (holds 10 slot elements)
└── Crafting Side
    ├── First Spell Container
    │   ├── Ability Rune Left Grid
    │   ├── Element Left Craft Grid
    │   ├── Effect/Impact Left Craft Grid (5 slots)
    │   └── Craft Result Left Grid
    ├── Second Spell Container (same structure)
    └── Third Spell Container (same structure)
```

### Step 2: Add SpellCraftingUI Component

1. Select the **Canvas** GameObject in Dev3Testing
2. Click **Add Component** → Search for "SpellCraftingUI"
3. Configure the following fields in the Inspector:

| Field | Value | Notes |
|-------|-------|-------|
| Crafting Panel | [Drag Canvas] | The entire UI panel to show/hide |
| Inventory Grid Parent | Drag **"Inventory Grid"** child | Will populate with available runes |
| Crafting Panels | Leave empty or set to 3 | Will auto-find CraftingRecipePanels |
| **Close Button** | **[Create new button - see below]** | Shows after successful craft |
| Main Inventory Side | [Optional] | Reference to left inventory panel |
| Crafting Side | [Optional] | Reference to right crafting panel |
| Toggle Key | Tab | Press Tab to open/close UI |

#### How to Create the Close Button:
1. **Right-click** on the **Canvas** GameObject in Hierarchy
2. **UI → Button** (creates a new Button child)
3. **Rename** the new Button to "CloseButton"
4. **Position** it somewhere visible (e.g., top-right of screen)
5. **Drag** this Button into the **Close Button** field in SpellCraftingUI inspector
6. **Optional**: Change the Button's text to "Close" in its Text child component

### Step 3: Add CraftingRecipePanel to Each Spell Container

For **each of the 3 spell containers** in Crafting Side:

1. Select the spell container
2. Click **Add Component** → Search for "CraftingRecipePanel"
3. Configure inspector fields:

| Field | Value |
|-------|-------|
| Ability Rune Grid Parent | Drag the **"Ability Rune Left Grid"** child |
| Element Rune Grid Parent | Drag the **"Element Left Craft Grid"** child |
| Modifier Rune Grid Parent | Drag the **"Effect/Impact Left Craft Grid"** child (5 slots) |
| **Craft Button** | **[Create new button - see below]** |
| **Recipe Status Text** | **[Create new text - see below]** |
| Target Slot | BasicAttack (0), Dash (1), or Shield (2) |

#### How to Create the Craft Button:
1. **Right-click** on the **spell container** GameObject in Hierarchy
2. **UI → Button** (creates a new Button child)
3. **Rename** the new Button to "CraftButton"
4. **Position** it near the bottom of the spell container
5. **Drag** this Button into the **Craft Button** field in CraftingRecipePanel inspector
6. **Optional**: Change the Button's text to "Craft Spell" in its Text child component

#### How to Create the Recipe Status Text:
1. **Right-click** on the **spell container** GameObject in Hierarchy
2. **UI → Legacy → Text** (creates a new Text child)
3. **Rename** the new Text to "StatusText"
4. **Position** it above or below the Craft Button
5. **Drag** this Text into the **Recipe Status Text** field in CraftingRecipePanel inspector
6. **Optional**: Set the Text component's default text to "Select runes to craft"

### Step 4: Create UI Prefabs (Optional)

For cleaner hierarchy, create RuneSlot prefab:

1. Create empty GameObject named "RuneSlot"
2. Add these components:
   - [Image] for icon display
   - [Text] for amount display
   - [Button] for click interaction
   - [RuneSlotUI] script reference
3. Save as `Assets/Resources/UI/RuneSlot.prefab`

The system will automatically use this prefab if it exists.

### Step 5: Verify Scene Requirements

Check that your Dev3Testing scene has these components (most should already exist):

1. **SpellCrafter Component**:
   - Should be on the same GameObject as AttunementSystem
   - If missing: Add component to Player or main game manager

2. **GameStateManager Component**:
   - Should exist in scene (creates VolatileRunState)
   - If missing: Create empty GameObject → Add GameStateManager

3. **PlayerController with Spell Slots**:
   - Should have AttunementSystem component
   - If missing: Add to Player GameObject

4. **Rune Seeding** (for testing):
   - Add DebugRuneSeeder component to any GameObject
   - Or manually add runes via code (see Example Rune Setup below)

### Step 6: Test the System

1. **Enter Play Mode** in Unity
2. **Press Tab key** → Crafting UI should open and game should pause
3. **Check Inventory**: Left side should show available runes (if seeded)
4. **Select Runes**: Click runes in inventory, they should appear in crafting slots
5. **Craft Spell**: Click "Craft Spell" button when recipe is valid
6. **Verify Success**: Close button should become enabled
7. **Close UI**: Press Tab or click Close button → Game should resume

#### If UI Doesn't Open:
- Check Console for errors
- Verify SpellCraftingUI component is on Canvas
- Ensure Crafting Panel field is assigned to Canvas
- Check that Time.timeScale is set to 0 (game paused)

#### If No Runes Appear:
- **Option 1 - Use DebugRuneSeeder**:
  1. Create empty GameObject → Name it "RuneSeeder"
  2. Add component "DebugRuneSeeder"
  3. The seeder will automatically add test runes on scene start
- **Option 2 - Manual seeding**: Add code to seed runes (see Example Rune Setup)

## Example Rune Setup

### User Experience

1. **Open UI**: Press **Tab** key → Game pauses → Inventory and crafting panels display
2. **Select Runes**: Click on runes in inventory to "preview" them in crafting slots
3. **Validate Recipe**: UI shows "Recipe Complete" when Ability Rune selected
4. **Craft Spell**: Click Craft button → SpellCrafter.TryCreate runs
5. **Allocation**: Runes are allocated into the recipe, counts update
6. **Close UI**: Close button enabled → Player presses Tab or clicks Close → Game resumes

### Data Flow

```
User clicks inventory rune
    ↓
OnAbilitySelected() / OnElementSelected()
    ↓
ValidateRecipe() checks: ability != null
    ↓
Craft button enabled/disabled
    ↓
User clicks Craft
    ↓
OnCraftButtonClicked()
    ↓
SpellCrafter.TryCreate(recipe, targetSlot)
    ↓
Success: RunState.AllocateRune() +  notification
Failure: Debug warning + UI stays open
    ↓
NotifyCraftSuccessful() enables Close button
```

## VolatileRunState Methods Used

```csharp
// Read what runes are available (not allocated)
public int AvailableCount(RuneDefinitionSO rune)

// Read-only inventory (all runes owned)
public IReadOnlyDictionary<RuneDefinitionSO, int> RuneInventory { get; }

// Read-only allocated runes (in active spells)
public IReadOnlyDictionary<RuneDefinitionSO, int> RuneAllocated { get; }

// Get spell currently in a slot
public ISpellSlot GetSlot(SlotIndex slot)
```

All allocation/deallocation is handled by SpellCrafter internally.

## Recipe Validation

Current validation logic:
- ✓ **Ability Rune**: REQUIRED
- ✓ **Element Rune**: OPTIONAL
- ✓ **Modifiers**: OPTIONAL (0-5 slots)

To change this, edit `CraftingRecipePanel.ValidateRecipe()`:

```csharp
private void ValidateRecipe()
{
    bool isValid = _selectedAbility != null;  // Add more conditions here
    // ... rest of validation
}
```

## Troubleshooting

### UI doesn't appear when Tab is pressed
- Check SpellCraftingUI is attached to Canvas
- Verify craftingPanel field is assigned
- Ensure Time.timeScale actually reaches 0 (check console logs)

### Close button is always disabled
- Verify SpellCrafter component exists and is accessible
- Check that TryCreate is actually being called
- Verify runes have proper allocation counts

### Runes not displaying
- Verify RuneInventory has runes (add via DebugRuneSeeder)
- Check AvailableCount returns > 0
- Ensure RuneSlotUI prefab OR inline creation works

### Recipe always fails TryCreate
- Verify recipe has valid Ability Rune
- Check SpellCrafter.TryCreate validation logic
- Ensure enough runes are unallocated (AvailableCount > 0)

## Example Rune Setup

In your DebugRuneSeeder, add runes like:

```csharp
// Add some ability runes
var projectileAbility = Resources.Load<AbilityRuneSO>("Runes/ProjectileAbility");
RunState.AddRune(projectileAbility, 2); // x2 available

// Add element runes
var fireElement = Resources.Load<ElementRuneSO>("Runes/FireElement");
RunState.AddRune(fireElement, 3); // x3 available

// Add modifier runes
var damageModifier = Resources.Load<ModifierRuneSO>("Runes/DamageBoost");
RunState.AddRune(damageModifier, 5); // x5 available
```

Then the UI will display these in the inventory grid.

## Advanced: Custom Recipe Validation

To add more complex validation (e.g., require Element), modify CraftingRecipePanel:

```csharp
private void ValidateRecipe()
{
    // Example: Require both Ability AND Element
    bool isValid = _selectedAbility != null && _selectedElement != null;
    
    if (craftButton != null)
        craftButton.interactable = isValid;
    
    if (recipeStatusText != null)
    {
        if (!isValid)
        {
            string missing = "";
            if (_selectedAbility == null) missing += "Ability ";
            if (_selectedElement == null) missing += "Element ";
            recipeStatusText.text = $"Missing: {missing}";
        }
        else
        {
            recipeStatusText.text = "Complete";
        }
    }
}
```

## Next Steps

1. Add the components to Dev3Testing scene
2. Place some runes in VolatileRunState (via seeder or rewards)
3. Test by pressing Tab to open UI
4. Click on runes and craft a spell
5. Verify Time.timeScale resumes when UI closes

---

**Created Scripts**:
- [RuneSlotUI.cs](../RuneSlotUI.cs)
- [CraftingRecipePanel.cs](../CraftingRecipePanel.cs)
- [SpellCraftingUI.cs](../SpellCraftingUI.cs)
