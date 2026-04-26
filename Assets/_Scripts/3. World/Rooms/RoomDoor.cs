using System;
using UnityEngine;

namespace World
{
    public class RoomDoor : MonoBehaviour
    {
        public event Action<Collider> OnPlayerEnter;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                OnPlayerEnter?.Invoke(other);
        }
    }
}