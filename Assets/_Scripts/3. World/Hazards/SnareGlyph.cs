using System.Collections;
using UnityEngine;
using Core;
using Foundation;

namespace World
{
    public sealed class SnareGlyph : MonoBehaviour , IHazard
    {
        [SerializeField] private float _snareDuration = 3f;
        [SerializeField] private GameObject _activateObject;

        private readonly int _disappearHash = Animator.StringToHash("t_Disappear");
        
        private bool _isActive = true;
        private Collider _collider;
        private Animator _anim;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _anim = _activateObject.GetComponent<Animator>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive)
                return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                return;

            // Dashing — hurtbox off, skip entirely. Glyph remains active.
            if (!player.Hurtbox.activeSelf)
                return;

            // Disable collider immediately — single-use, no re-triggering.
            _collider.enabled = false;

            // Shielding — glyph triggers, shield is destroyed, player is not snared.
            if (player.IsShielding)
            {
                player.ForceDestroyActiveShield();
                gameObject.SetActive(false);
                return;
            }

            StartCoroutine(SnareRoutine(player));
        }

        private IEnumerator SnareRoutine(PlayerController player)
        {
            _activateObject.SetActive(true);
            
            // Player cannot move but can still cast — SetCanMove only blocks HandleMovement.
            player.SetCanMove(false);
            player.SetVelocity(Vector3.zero);
            player.TeleportTo(new PlayerTeleportRequestEvent(transform.position - Vector3.forward * 0.5f));

            yield return CoroutineUtils.GetWait(_snareDuration);
            
            _anim.SetTrigger(_disappearHash);

            // Null check — player could have died during the snare duration.
            if (player != null)
                player.SetCanMove(true);

            //Animation disappearing duration.
            Destroy(gameObject, 0.7f);
        }
        
        public void Disable()
        {
            _isActive = false;
            _activateObject.SetActive(true);
            _anim.SetTrigger(_disappearHash);
            Destroy(gameObject, 0.7f);
        }
    }
}