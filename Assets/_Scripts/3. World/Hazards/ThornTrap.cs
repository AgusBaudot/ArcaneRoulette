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

            if (other.GetComponentInParent<IDamageable>() == null && other.GetComponentInParent<PlayerController>() == null)
                    return;

            _isIdle = false;
            StartCoroutine(TrapRoutine());
        }

        private IEnumerator TrapRoutine()
        {
            yield return CoroutineUtils.GetWait(_windupDuration);

            _anim.SetTrigger(_activateHash);
            
            ApplyDamage();

            yield return CoroutineUtils.GetWait(_spikeDisplayDuration);
            _anim.SetTrigger(_cooldownHash);

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
                if (!processed.Add(damageable)) continue;

                var go = (damageable as Component)?.gameObject;
                var player = go?.GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    if (!player.Hurtbox.activeSelf) continue;

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
            _anim.SetTrigger(_cooldownHash);
            _isActive = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, _boxSize * 2);
        }
    }
}