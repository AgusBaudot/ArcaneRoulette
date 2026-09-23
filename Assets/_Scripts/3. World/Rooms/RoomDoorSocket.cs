using UnityEngine;

namespace World
{
    public sealed class RoomDoorSocket : MonoBehaviour
    {
        [SerializeField] private GameObject _doorVisual;
        [SerializeField] private GameObject _wallVisual;
        [SerializeField] private Collider _doorTrigger;

        public void SetOpen(bool isOpen)
        {
            if (_doorVisual != null) _doorVisual.SetActive(isOpen);
            if (_wallVisual != null) _wallVisual.SetActive(!isOpen);
            if (_doorTrigger != null) _doorTrigger.enabled = isOpen;
        }

        public void SetLocked(bool isLocked)
        {
            if (_doorTrigger != null) _doorTrigger.enabled = !isLocked;
        }
    }
}