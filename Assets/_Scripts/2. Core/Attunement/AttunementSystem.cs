using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(SpellCrafter))]
    public class AttunementSystem : MonoBehaviour
    {
        private SpellCrafter _crafter;

        private void Awake()
        {
            _crafter = GetComponent<SpellCrafter>();
        }

        private void Start()
        {
            RestoreSpellsFromRunState();
        }

        private void RestoreSpellsFromRunState()
        {
            var state = GameStateManager.RunState;
            if (state == null) return;

            // Loop through the 3 slots using the enum
            for (int i = 0; i < 3; i++)
            {
                SlotIndex currentSlotIndex = (SlotIndex)i;

                // 1. Correctly use GetSlot() instead of the non-existent .Slots property
                ISpellSlot previousSlot = state.GetSlot(currentSlotIndex); 

                if (previousSlot is SpellInstance previousInstance)
                {
                    // 2. Pass the SlotIndex enum. 
                    // Note: TryCreate automatically calls _attunement.Bind() upon success, 
                    // so we do not need to call it manually here.
                    _crafter.TryCreate(previousInstance.Recipe, currentSlotIndex, out SpellInstance _);
                }
            }
        }

        // 3. Changed 'int' to 'SlotIndex' and 'SpellInstance' to 'ISpellSlot'.
        // This allows SpellCrafter.Dismantle to pass null to clear the slot.
        public void Bind(SlotIndex slotIndex, ISpellSlot instance)
        {
            GameStateManager.RunState.SetSlot(slotIndex, instance);
            EventBus.Publish(new SpellEquippedEvent(slotIndex, instance));
        }
    }
}