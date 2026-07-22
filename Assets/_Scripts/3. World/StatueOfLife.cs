using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class StatueOfLife : RestingStatue
    {
        protected override void ApplyReward(GameObject player)
        {
            if (player.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.Heal(Mathf.RoundToInt(GameStateManager.RunState.MaxHp * 0.2f));
                
                //TODO: Fire AudioPlayRequest for the healing SFX here.
            }
        }
    }
}