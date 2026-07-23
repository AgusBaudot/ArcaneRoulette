using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class StatueOfLife : RestingStatue
    {
        [SerializeField, Range(0, 1)] private float _healthAmount;
        
        protected override void ApplyReward(GameObject player)
        {
            if (player.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.Heal(Mathf.RoundToInt(GameStateManager.RunState.MaxHp * _healthAmount));
            }
        }
    }
}