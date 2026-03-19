namespace Foundation
{
    //Mutable by design - modifiers write to these fields.
    //All context passed by reference through the pipeline.
    
    public class DamageContext
    {
        public float BaseDamage;
        public float FinalDamage; //modifiers write here
        public ElementType Element;
        public bool IsCrit;
        public bool Cancelled; //an artifact can negate damage entirely
    }

    public class KillContext
    {
        public ElementType KillingElement;
        public bool WasCrit;
        //future: enemy type, position for loot drops
    }

    public class SpellContext
    {
        public SlotIndex Slot;
        public ElementType Element;
        public float CooldownMultiplier; //Modifiers can reduce cooldown
    }

    public class DashContext
    {
        public float DistanceMultiplier; //artifacts: "dash further"
        public bool LeaveTrail; //artifacts: "dash leaves fire"
        public ElementType TrailElement;
    }
}