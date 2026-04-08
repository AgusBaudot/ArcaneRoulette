namespace Foundation
{
    /// <summary>
    /// Implemented by any artifact or effect that modifies gameplay events.
    /// InventorySystem calls Register/Unregister as artifacts enter/leave a run.
    /// Implementations subscribe their own delegates to VolatileRuneState
    /// pipeline events in Register, and unsubscribe in Unregister.
    /// </summary>
    public interface IModifier
    {
        void Register();
        void Unregister();
    }
}