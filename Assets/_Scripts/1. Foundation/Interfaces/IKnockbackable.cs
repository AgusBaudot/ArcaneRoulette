using UnityEngine;

namespace Foundation
{
    public interface IKnockbackable
    {
        void ApplyKnockback(Vector3 direction, float force);
    }
}