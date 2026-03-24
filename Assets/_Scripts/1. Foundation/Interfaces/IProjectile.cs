using UnityEngine;

namespace Foundation
{
    public interface IProjectile
    {
        Rigidbody Rb { get; }
        bool IsEnemy { get; } //false for player projectiles
    }
}