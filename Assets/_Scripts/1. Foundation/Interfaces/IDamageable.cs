namespace Foundation
{
    public interface IDamageable 
    {
        /// <summary>
        /// Receives final resolved damage. No further calculation here.
        /// DamageSystem is responsible for resistance and modifier hooks
        /// before this is called.
        /// Returns tru if damage was taken, false if ignored/blocked
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="elementType"></param>
        bool TakeDamage(int amount, ElementType elementType);
    }
}