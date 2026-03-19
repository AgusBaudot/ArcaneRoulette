using UnityEngine;

namespace Core
{
    public interface IHoldAbility
    {
        void OnPressed (PlayerController player, Vector2 direction);
        void OnHeld (PlayerController player, Vector2 direction);
        void OnReleased (PlayerController player);
    }
}