using Foundation;

namespace Core
{
    public class DoTInstance
    {
        public int Damage { get; private set; }
        public float RemainingDuration { get; set; }
        public float TickInterval { get; private set; }
        public float TimeUntilNextTick { get; set; }
        public ElementType Element { get; private set; }

        public DoTInstance(int damage, float duration, float tickInterval, ElementType element)
        {
            Damage = damage;
            RemainingDuration = duration;
            TickInterval = tickInterval;
            TimeUntilNextTick = tickInterval; // First damage tick happens after the interval
            Element = element;
        }

        // Future-proofing: Allows an Artifact to grab an existing DoT and copy it to another enemy
        public DoTInstance Clone()
        {
            return new DoTInstance(Damage, RemainingDuration, TickInterval, Element)
            {
                TimeUntilNextTick = this.TimeUntilNextTick
            };
        }
    }
}