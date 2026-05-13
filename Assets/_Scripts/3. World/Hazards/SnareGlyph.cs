using System.Collections;
using UnityEngine;
using Core;

namespace World
{
    public sealed class SnareGlyph : MonoBehaviour , IHazard
    {
        [SerializeField] private float _snareDuration = 3f;
        [SerializeField] private GameObject _activateObject;

        private bool _isActive = true;

        private Collider _collider;

        public void Disable()
        {
            _isActive = true;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive)
                return;

            // Only the player triggers this glyph.
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            // Dashing — hurtbox off, skip entirely. Glyph remains active.
            if (!player.Hurtbox.activeSelf) return;

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

            yield return CoroutineUtils.GetWait(_snareDuration);

            // Null check — player could have died during the snare duration.
            if (player != null)
                player.SetCanMove(true);

            Destroy(gameObject);
        }
    }
}