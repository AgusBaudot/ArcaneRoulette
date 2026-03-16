using System;
using UnityEngine;

namespace Core
{
    public class FireShield : MonoBehaviour, IAbility
    {
        private PlayerController _player;

        private float _shieldTimer;
        private float _shieldCooldownTimer;

        private bool _isShielding;

        private void Update()
        {
            _shieldCooldownTimer -= Time.deltaTime;
            if (_isShielding)
                HandleShield();
        }

        public void Execute(PlayerController player, Vector2 inputDirection)
        {
            if (_isShielding || _shieldCooldownTimer > 0f) return;

            _player = player;
            StartShield();
        }

        private void StartShield()
        {
            _isShielding = true;

            _shieldTimer = _player.Stats.ShieldDuration;
            _shieldCooldownTimer = _player.Stats.ShieldCooldown;
        }

        private void HandleShield()
        {
            _shieldTimer -= Time.deltaTime;
            
            if (_shieldTimer <= 0f)
            {
                _isShielding = false;
            }
        }
    }
}