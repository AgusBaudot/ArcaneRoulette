using UnityEngine;

namespace World
{
    [RequireComponent(typeof(BoxCollider))]
    public class TeleportZone : MonoBehaviour
    {
        public BoxCollider Collider { get; private set; }

        private void Awake()
        {
            Collider = GetComponent<BoxCollider>();
            Collider.isTrigger = true; 
        }
    }
}