namespace Foundation
{
    /// <summary>
    /// Runtime snapshot of an equipped spell. Pure data, no behaviour.
    /// Created by AttunementSystem from a SpellDefinition SO.
    /// Foundation holds this so VolatileRunState can reference it legally.
    /// </summary>
    public class SpellInstance
    {
        public SlotIndex Slot;
        public ElementType Element;
        public int Level; //1 = base, > 1 = upgraded.
        
        //Baked stats - copied from SpellDefinition at bind time
        public float Damage;
        public float Cooldown;
        public float ProjectileSpeed;
        public float Range;
        
        //Modifier pipeline writes here at cast time
        public float CooldownMultiplier = 1f;
    }
}