using UnityEngine;

namespace Core
{
    public class WindDash : MonoBehaviour, IAbility
    {
        private PlayerController _player;

        private Vector3 _dashDirection;
        
        private float _dashTimer;
        private float _dashCooldownTimer;

        private bool _isDashing;

        private void Update()
        {
            _dashCooldownTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (_isDashing) HandleDash();
        }

        public void Execute(PlayerController player, Vector2 inputDirection)
        {
            if (_isDashing || _dashCooldownTimer > 0f) return;
            
            _player = player;
            StartDash(inputDirection);
        }
        
        private void StartDash(Vector2 direction)
        {
            _isDashing = true;
            _player.SetCanMove(false);
            
            _dashTimer = _player.Stats.DashDuration;
            _dashCooldownTimer = _player.Stats.DashCooldown;
            
            // Input is 2D (XY) — project onto world XZ plane
            _dashDirection = new Vector3(direction.x, 0f, direction.y).normalized;
        }
        
        private void HandleDash()
        {
            _dashTimer -= Time.fixedDeltaTime;
            _player.Rigidbody.velocity = _dashDirection * _player.Stats.DashSpeed;

            if (_dashTimer <= 0f)
            {
                _player.SetCanMove(true);
                _isDashing = false;
            }
        }
    }
}