using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    public class SpellCrafter : MonoBehaviour
    {
        private VolatileRunState RunState => GameStateManager.RunState;
        private AttunementSystem _attunement;

        private void Awake()
        {
            _attunement = GetComponent<AttunementSystem>();
        }

        public bool TryCreate(SpellRecipe recipe, SlotIndex slot, out SpellInstance result)
        {
            result = null;

            // 1. Recipe must have an ability rune
            if (!recipe.IsValid)
            {
                Debug.LogWarning("SpellCrafter: recipe has no ability rune.");
                return false;
            }

            // 2. Tally what is currently in the slot, as these will be freed if we succeed.
            var freedByDismantle = new Dictionary<RuneDefinitionSO, int>();
            var current = RunState.GetSlot(slot) as SpellInstance;
            
            if (current != null)
            {
                void CountFreed(RuneDefinitionSO rune)
                {
                    if (rune == null) return;
                    freedByDismantle.TryGetValue(rune, out int c);
                    freedByDismantle[rune] = c + 1;
                }
                
                CountFreed(current.Recipe.Ability);
                CountFreed(current.Recipe.Element);
                foreach (var mod in current.Recipe.Modifiers)
                    CountFreed(mod);
            }

            // 3. Tally what the new recipe needs.
            var needed = new Dictionary<RuneDefinitionSO, int>();
            void CountNeeded(RuneDefinitionSO rune)
            {
                if (rune == null) return;
                needed.TryGetValue(rune, out int c);
                needed[rune] = c + 1;
            }
            
            CountNeeded(recipe.Ability);
            CountNeeded(recipe.Element);
            foreach (var mod in recipe.Modifiers)
                CountNeeded(mod);

            // 4. Validate Effective Availability
            foreach (var pair in needed)
            {
                // Effective Available = Currently Available + What we are about to free from this slot
                freedByDismantle.TryGetValue(pair.Key, out int freedCount);
                int effectiveAvailable = RunState.AvailableCount(pair.Key) + freedCount;

                if (effectiveAvailable < pair.Value)
                {
                    Debug.LogWarning($"SpellCrafter: not enough {pair.Key.name}.");
                    return false;
                }
            }
            
            // 5. Validation passed! Safe to dismantle the old spell.
            Dismantle(slot);
            
            // 6. Allocate every rune in the new recipe.
            foreach (var pair in needed)
                RunState.AllocateRune(pair.Key, pair.Value);
            
            // 7. Construct - IsHoldAbility on the rune decides the class.
            result = recipe.Ability.IsHoldAbility ? new HoldSpellInstance(recipe) : new SpellInstance(recipe);
            
            // 8. Bind into the slot and notify PlayerController via bus.
            _attunement.Bind(slot, result);
            EventBus.Publish(new SpellCraftedEvent(slot, result));
            
            return true;
        }

        public void Dismantle(SlotIndex slot)
        {
            //Read what's currently in the slot
            var current = RunState.GetSlot(slot) as SpellInstance;
            if (current == null)
                return;
            
            //Deallocate every rune in its recipe
            var recipe = current.Recipe;

            void Free(RuneDefinitionSO rune)
            {
                if (rune != null)
                    RunState.DeallocateRune(rune);
            }
            
            Free(recipe.Ability);
            Free(recipe.Element);
            
            //Modifiers may contain duplicates - deallocate once per slot entry,
            //not once per unique rune, to mirror how AllocateRune counted them.
            foreach (var mod in recipe.Modifiers)
                Free(mod);

            if (current.Recipe.Ability is ShieldAbilityRune shieldRune)
                shieldRune.CleanupInstance(current);

            _attunement.Bind(slot, null);
            EventBus.Publish(new SpellDismantledEvent(slot));
        }
    }
}