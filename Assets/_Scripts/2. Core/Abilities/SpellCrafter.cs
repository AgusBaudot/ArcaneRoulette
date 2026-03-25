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

            //1. Recipe must have an ability rune
            if (!recipe.IsValid)
            {
                Debug.LogWarning("SpellCrafter: recipe has no ability rune.");
                return false;
            }

            //2. Check availability for every rune in the recipe.
            //We count how many times each rune appears in this recipe first,
            //then compare against what's available.
            //This handles the case where the same rune fills two modifier slots.
            var needed = new Dictionary<RuneDefinitionSO, int>();

            void Count(RuneDefinitionSO rune)
            {
                if (rune == null)
                    return;
                needed.TryGetValue(rune, out int c);
                needed[rune] = c + 1;
            }
            
            Count(recipe.Ability);
            Count(recipe.Element);
            foreach (var mod in recipe.Modifiers)
                Count(mod);

            foreach (var pair in needed)
            {
                if (RunState.AvailableCount(pair.Key) < pair.Value)
                {
                    Debug.LogWarning($"SpellCrafter: not enough {pair.Key.name}.");
                    return false;
                }
            }
            
            //3. Dismantle whatever is currently in this slot first.
            //Frees its allocation before we allocate the new recipe.
            Dismantle(slot);
            
            //4. Allocate every rune in the new recipe.
            foreach (var pair in needed)
                RunState.AllocateRune(pair.Key, pair.Value);
            
            //5. Construct - IsHoldAbility on the rune decides the class.
            result = recipe.Ability.IsHoldAbility ? new HoldSpellInstance(recipe) : new SpellInstance(recipe);
            
            //6. Bind into the slot and notify PlayerController via bus.
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

            _attunement.Bind(slot, null);
            EventBus.Publish(new SpellDismantledEvent(slot));
        }
    }
}