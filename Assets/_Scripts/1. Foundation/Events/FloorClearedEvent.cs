namespace Foundation
{
    /// <summary>
    /// Fired when a floor is fully cleared, and it's time to transition onward.
    /// No payload — floor number lives on VolatileRunState, not the event.
    /// Interim publisher: Portal (PortalType.NextFloor). Once FloorManager exists,
    /// authority for "is the floor actually done" should move there instead —
    /// Portal is a player-facing trigger, not proof every room (boss included)
    /// is cleared.
    /// </summary>
    public readonly struct FloorClearedEvent
    { }
}