using UnityEngine;
using Foundation;

namespace World
{
    public class TutorialDoor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [Tooltip("The solid collider that prevents the player from walking through.")]
        [SerializeField] private Collider _physicalCollider; 
        [Tooltip("The trigger volume that actually teleports the player.")]
        [SerializeField] private Collider _teleportTrigger;

        [Header("Audio")]
        [SerializeField] private AudioEventSO _doorOpenSound;
        [SerializeField] private AudioEventSO _doorCloseSound;

        private static readonly int UnlockHash = Animator.StringToHash("Unlock");
        private static readonly int LockHash = Animator.StringToHash("Lock");

        private void Awake()
        {
            Close(false);
        }

        public void Open(bool playSound = true)
        {
            if (_animator != null) _animator.SetTrigger(UnlockHash);
            if (_physicalCollider != null) _physicalCollider.enabled = false;
            if (_teleportTrigger != null) _teleportTrigger.enabled = true;

            if (_doorOpenSound != null && playSound)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _doorOpenSound, WorldPosition = transform.position });
            }
        }

        public void Close(bool playSound = true)
        {
            if (_animator != null) _animator.SetTrigger(LockHash);
            if (_physicalCollider != null) _physicalCollider.enabled = true;
            if (_teleportTrigger != null) _teleportTrigger.enabled = false;

            if (_doorCloseSound != null && playSound)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _doorCloseSound, WorldPosition = transform.position });
            }
        }
    }
}