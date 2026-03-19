using UnityEngine;

namespace Core
{
    public interface IAbility
    {
        void Execute(PlayerController player, Vector2 inputDirection);
    }
}
