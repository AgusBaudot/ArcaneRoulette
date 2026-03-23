using Foundation;
using UnityEngine;

namespace Core
{
    public class AttunementSystem : MonoBehaviour
    {
        private VolatileRunState RunState => GameStateManager.RunState;
        
        public void Bind(SlotIndex slot, SpellInstance instance)
        {
            //Write into the shared run state - OnSlotChanged fires here.
            //PlayerController hears it via SpellEquippedEvent on the bus.
            RunState.SetSlot(slot, instance);
            EventBus.Publish(new SpellEquippedEvent(slot, instance));
        }
    }
}