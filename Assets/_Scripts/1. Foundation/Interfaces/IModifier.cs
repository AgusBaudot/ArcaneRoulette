namespace Foundation
{
    /// <summary>
    /// Implemented by any artifact or effect that wants to intercept
    /// game events and mutate their context.
    /// InventorySystem calls Register/Unregister as artifacts are
    /// added or removed during a run.
    /// </summary>
    public interface IModifier
    {
        void Register(VolatileRunState state);
        void Unregister(VolatileRunState state);
    }
}