using System;
using UnityEngine;
using Foundation;

namespace Core
{
    public class ElectroDash : MonoBehaviour
    {
        [SerializeField] private GameObject _lightningPrefab;
        [SerializeField] private float _radius;
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.5f;

        private Vector3 _dashDirection;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private bool _isDashing;

        private PlayerController _player;

        //UpdateManager: replace with Tick() registration
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
            StartDash(player, inputDirection);
        }

        private void StartDash(PlayerController player, Vector2 direction)
        {
            _isDashing = true;
            _dashTimer = _player.Stats.DashDuration;
            _dashCooldownTimer = _player.Stats.DashCooldown;
            // Input is 2D (XY) — project onto world XZ plane
            _dashDirection = new Vector3(direction.x, 0f, direction.y).normalized;

            _player.SetCanMove(false);
            _player.Hurtbox.SetActive(false); //Invincible for dash duration

            SummonLightning(player);
        }

        private void HandleDash()
        {
            _dashTimer -= Time.fixedDeltaTime;
            _player.Rigidbody.velocity = _dashDirection * _player.Stats.DashSpeed;

            if (_dashTimer <= 0f)
            {
                _isDashing = false;
                _player.SetCanMove(true);
                _player.Hurtbox.SetActive(true); //vulnerable again
                _player = null;
            }
        }

        private void SummonLightning(PlayerController player)
        {
            var spawnPosition = new Vector3(player.transform.position.x, _lightningPrefab.transform.position.y, player.transform.position.z);
            Instantiate(_lightningPrefab, spawnPosition, _lightningPrefab.transform.rotation);
            
            var enemies = Physics.OverlapSphere(_player.transform.position, _radius, _player.Stats.EnemyLayerMask);
            bool hitAny = false;
            
            foreach (var enemy in enemies)
            {
                if (!enemy.gameObject.TryGetComponent<IDamageable>(out var damageable)) 
                    continue;

                damageable.TakeDamage(_player.Stats.BaseDamage, ElementType.Electric);

                //Enemy is stunned?

                //Damage flash
                if (enemy.gameObject.TryGetComponent<DamageFlash>(out var flash))
                    flash.Flash();

                hitAny = true;
            }

            if (hitAny)
            {
                HitStop.Apply(_hitStopDuration);
                CameraShake.AddTrauma(_cameraTrauma);
            }
            else
            {
                CameraShake.AddTrauma(_cameraTrauma * 0.5f);
            }
        }
    }
}