namespace Foundation
{
    /// <summary>
    /// Base class for cast-phase modifier runes.
    ///
    /// Subscribe is called once at SpellInstance construction. Implementations
    /// subscribe typed delegates to the appropriate ability hook events and push
    /// the corresponding unsubscribe closures into cleanupList. SpellInstance
    /// drains cleanupList on Cleanup() (called by SpellCrafter.Dismantle).
    ///
    /// No state is stored on the SO. Stack count closes over the int parameter
    /// at subscribe time and never changes for a given SpellInstance lifetime.
    /// The SO is therefore safe to reference from any number of concurrent
    /// SpellInstances without mutable shared state.
    ///
    /// Apply — DELETED. Replaced by Subscribe / cleanup-list pattern.
    /// </summary>
    public abstract class CastRuneSO : ModifierRuneSO
    {
        /// <param name="ability">The ability rune this spell is built around.</param>
        /// <param name="stackCount">Number of times this rune appears in the recipe.</param>
        /// <param name="cleanupList">
        /// List owned by the SpellInstance. Push one Action per subscribed
        /// delegate — each Action must perform the matching unsubscription.
        /// </param>
        public abstract void Subscribe(
            AbilityRuneSO ability,
            int stackCount,
            System.Collections.Generic.List<System.Action> cleanupList);
    }
}