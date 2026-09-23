using UnityEngine;

namespace Foundation
{
    /// <summary>
    /// Two-state lifecycle for a destructible prop. Every implementer starts in
    /// Unbroken and transitions to Destroyed exactly once, via IDestructible.OnDeath.
    /// </summary>
    public enum DestructibleState
    {
        Unbroken,
        Destroyed
    }

    /// <summary>
    /// Contract for anything that can be broken by an Impact. The caller resolves
    /// the hit and calls OnDeath; the implementer decides what breaking means.
    /// </summary>
    public interface IDestructible
    {
        DestructibleState State { get; }
        bool IsDestroyed { get; }

        /// <summary>
        /// Resolves an Impact against this object — every impact is fatal, there's
        /// no partial-damage state. Must be idempotent: a second call after the
        /// object is already Destroyed (e.g. an AoE secondary hit reaching an
        /// already-broken prop) is a no-op, not a second drop roll.
        /// </summary>
        void OnDeath(Vector3 hitPosition);
    }
}