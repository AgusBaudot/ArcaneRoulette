using UnityEngine;

namespace Foundation
{
    /// <summary>
    /// Implemented by specialized projectiles (like the Healer's bottle) that 
    /// require bespoke reflection logic rather than the standard projectile spread.
    /// </summary>
    public interface ICustomReflectable
    {
        bool TryCustomReflect(Vector3 reflectDir, int bounceRunes, IStatResolver playerStats);
    }
}