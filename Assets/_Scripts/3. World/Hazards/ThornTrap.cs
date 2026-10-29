using System.Collections;
using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class ThornTrap : MonoBehaviour, IHazard
    {
        [Header("Stats")] 
        [SerializeField] private int _damage = 15;
        [SerializeField] private float _windupDuration = 0.5f;
        [SerializeField] private float _spikeDisplayDuration = 0.3f;
        [SerializeField] private float _cooldownDuration = 2f;
        [SerializeField] private Vector3 _boxSize;

        [Header("Animation")] 
        [SerializeField] private Animator _anim;

        [Header("Audio")]
        [SerializeField] private AudioEventSO _activateSound;
        [SerializeField] private AudioEventSO _deactivateSound;

        private bool _isActive = true;
        private bool _isIdle = true;
        private Coroutine _activeRoutine;

        private readonly int _activateHash = Animator.StringToHash("Activate_Up");
        private readonly int _cooldownHash = Animator.StringToHash("Activate_Cooldown");
        private readonly int _idleHash = Animator.StringToHash("Activate_Idle");

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive)
                return;

            if (!_isIdle)
                return;

            if (other.GetComponentInParent<IDamageable>() == null &&
                other.GetComponentInParent<PlayerController>() == null)
                return;

            _isIdle = false;
            _activeRoutine = StartCoroutine(TrapRoutine());
        }

        private IEnumerator TrapRoutine()
        {
            yield return CoroutineUtils.GetWait(_windupDuration);

            _anim.SetTrigger(_activateHash);
            if (_activateSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _activateSound, WorldPosition = transform.position });
            }

            ApplyDamage();

            yield return CoroutineUtils.GetWait(_spikeDisplayDuration);
            
            _anim.SetTrigger(_cooldownHash);
            if (_deactivateSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _deactivateSound, WorldPosition = transform.position });
            }

            yield return CoroutineUtils.GetWait(_cooldownDuration);
            _anim.SetTrigger(_idleHash);

            _isIdle = true;
            _activeRoutine = null;
        }

        private void ApplyDamage()
        {
            var hits = Physics.OverlapBox(transform.position, _boxSize);
            var processed = new HashSet<IDamageable>();

            var batch = new DamageBatch();
            bool hitPlayer = false;

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>()
                                 ?? hit.GetComponent<IDamageable>();
                
                if (damageable == null) 
                    continue;
                
                if (!processed.Add(damageable)) 
                    continue;

                var go = (damageable as Component)?.gameObject;
                var player = go?.GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    if (!player.Hurtbox.activeSelf) 
                        continue;

                    if (player.IsShielding)
                    {
                        player.ForceDestroyActiveShield();
                        continue;
                    }

                    hitPlayer = true;
                }

                batch.Deal(damageable, go, _damage, ElementType.Neutral);
            }

            batch.Commit(hitPlayer ? Helpers.Combat.PlayerDamage : Helpers.Combat.BigDMG);
        }

        public void Disable()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }

            _anim.SetTrigger(_cooldownHash);
            if (_isActive && _deactivateSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _deactivateSound, WorldPosition = transform.position });
            }
            
            _isActive = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, _boxSize * 2);
        }
    }
}