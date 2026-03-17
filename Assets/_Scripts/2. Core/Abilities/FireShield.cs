using System;
using UnityEngine;

namespace Core
{
    public class FireShield : MonoBehaviour, IHoldAbility
    {
        [SerializeField] private GameObject _shieldVisual;

        private bool _active;

        public void OnPressed(PlayerController player, Vector2 direction)
        {
            if (!player.Energy.TryStartDrain()) return; //Broken or empty: silent reject.

            _active = true;
            if (_shieldVisual)
                _shieldVisual.SetActive(true);
        }

        public void OnHeld(PlayerController player, Vector2 direction)
        {
            if (!_active) return;
            
            //Energy.Tick() already draining - just watch for depletion.
            if (player.Energy.IsBroken)
                Deactivate(player);
        }

        public void OnReleased(PlayerController player)
        {
            if (_active) Deactivate(player);
        }

        private void Deactivate(PlayerController player)
        {
            _active = false;
            player.Energy.StopDrain();
            if (_shieldVisual)
                _shieldVisual.SetActive(false);
        }
    }
}