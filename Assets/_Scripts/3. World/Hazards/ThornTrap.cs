using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class ThornTrap : MonoBehaviour , IHazard
    {
        [Header("Stats")]
        [SerializeField] private int _damage = 15;
        [SerializeField] private float _windupDuration = 0.5f;
        [SerializeField] private float _spikeDisplayDuration = 0.3f;
        [SerializeField] private float _cooldownDuration = 2f;
        [SerializeField] private Vector3 _boxSize;

        [Header("Animation")]
        [SerializeField] private Animator _anim;

        private bool _isActive = true;
        private bool _isIdle = true;

        private readonly int _activateHash = Animator.StringToHash("Activate_Up");
        private readonly int _cooldownHash = Animator.StringToHash("Activate_Cooldown");
        private readonly int _idleHash = Animator.StringToHash("Activate_Idle");

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive)
                return;

            if (!_isIdle) return;

            // Only players and enemies (IDamageable entities) activate the trap.
            // Projectiles and other objects are ignored.
            if (other.GetComponentInParent<IDamageable>() == null && other.GetComponentInParent<PlayerController>() == null)
                    return;

            _isIdle = false;
            StartCoroutine(TrapRoutine());
        }

        private IEnumerator TrapRoutine()
        {
            // Windup — spikes partially emerge, telegraphs the hit
            yield return CoroutineUtils.GetWait(_windupDuration);

            // Spikes fully emerge — damage fires exactly here, one OverlapSphere
            //if (_spikesVisual != null) 
            //    _spikesVisual.SetActive(true);
            _anim.SetTrigger(_activateHash);
            
            ApplyDamage();

            // Spikes stay visible briefly for readability, then retract
            yield return CoroutineUtils.GetWait(_spikeDisplayDuration);
            _anim.SetTrigger(_cooldownHash);
            //if (_spikesVisual != null) _spikesVisual.SetActive(false);

            // Cooldown before trap can activate again
            yield return CoroutineUtils.GetWait(_cooldownDuration);
            _anim.SetTrigger(_idleHash);

            _isIdle = true;
        }

        private void ApplyDamage()
        {
            var hits = Physics.OverlapBox(transform.position, _boxSize);
            var processed = new HashSet<IDamageable>();

            var batch = new DamageBatch();

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>()
                                 ?? hit.GetComponent<IDamageable>();
                if (damageable == null) continue;
                if (!processed.Add(damageable)) continue; // dedup multi-collider enemies

                var go = (damageable as Component)?.gameObject;
                var player = go?.GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    // Dashing — hurtbox is off, skip entirely. No interaction.
                    if (!player.Hurtbox.activeSelf) continue;

                    // Shielding — trap triggers, shield is destroyed, no damage.
                    if (player.IsShielding)
                    {
                        player.ForceDestroyActiveShield();
                        continue;
                    }
                }
                
                batch.Deal(damageable, go, _damage, ElementType.Neutral);
            }
            
            batch.Commit(Helpers.Combat.BigDMG);
        }

        public void Disable()
        {
            _isActive = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, _boxSize * 2);
        }
    }
}