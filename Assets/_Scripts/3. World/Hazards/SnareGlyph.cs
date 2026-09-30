using System.Collections;
using UnityEngine;
using Core;
using Foundation;

namespace World
{
    public sealed class SnareGlyph : MonoBehaviour , IHazard
    {
        [SerializeField] private float _snareDuration = 3f;
        [SerializeField] private float _pullDelay = 0.5f;
        [SerializeField] private float _pullSpeed = 4f;
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
            _isActive = false;

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
            
            player.SetCanMove(false);
            player.SetVelocity(Vector3.zero);

            if (_pullDelay > 0f)
            {
                yield return CoroutineUtils.GetWait(_pullDelay);
            }
            
            Vector3 targetPos = transform.position - (Vector3.forward * 0.5f);
            float elapsed = 0f;
            
            float pullDuration = Mathf.Max(0f, _snareDuration - _pullDelay);
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            while (elapsed < pullDuration)
            {
                if (player == null || !player.gameObject.activeInHierarchy) 
                    break;

                Vector3 currentPos = player.transform.position;
                Vector3 diff = targetPos - currentPos;
                diff.y = 0f;

                float distance = diff.magnitude;

                if (distance > 0.01f)
                {
                    float currentSpeed = Mathf.Min(_pullSpeed, distance / Time.fixedDeltaTime);
                    player.SetVelocity(diff.normalized * currentSpeed);
                }
                else
                {
                    player.SetVelocity(Vector3.zero);
                }

                yield return wait;
                elapsed += Time.fixedDeltaTime;
            }
            
            _anim.SetTrigger(_disappearHash);

            if (player != null)
            {
                player.SetVelocity(Vector3.zero);
                player.SetCanMove(true);
            }

            // Animation disappearing duration.
            Destroy(gameObject, 0.7f);
        }
        
        public void Disable()
        {
            if (!_isActive)
                return;

            _isActive = false;
            _activateObject.SetActive(true);
            _anim.SetTrigger(_disappearHash);
            Destroy(gameObject, 0.7f);
        }
    }
}