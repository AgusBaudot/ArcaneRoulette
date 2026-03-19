using Foundation;

namespace Core
{
    public interface IDamageable 
    {
        /// <summary>
        /// Receives final resolved damage. No further calculation here.
        /// DamageSystem is responsible for resistance and modifier hooks
        /// before this is called.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="elementType"></param>
        void TakeDamage(int amount, ElementType elementType);
    }
}