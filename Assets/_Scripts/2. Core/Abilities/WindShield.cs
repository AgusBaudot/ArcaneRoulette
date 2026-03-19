using System;
using UnityEngine;

namespace Core
{
    public class WindShield : MonoBehaviour, IHoldAbility
    {
        [SerializeField] private GameObject _shieldVisual;
        [Tooltip("How much time does it take to spawn the shield's ability.")]
        [SerializeField] private float _abilityThreshold;
        [SerializeField] private GameObject _shockWavePrefab;

        private float _timeHeldShield;
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
            
            _timeHeldShield += Time.deltaTime;
            if (_timeHeldShield >= _abilityThreshold)
            {
                Instantiate(_shockWavePrefab, player.transform.position, Quaternion.identity);
                _timeHeldShield -= _abilityThreshold; //if held for more time, make it count for the next.
            }
            
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
            _timeHeldShield = 0;
            player.Energy.StopDrain();
            if (_shieldVisual)
                _shieldVisual.SetActive(false);
        }
    }
}