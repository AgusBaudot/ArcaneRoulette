using System;
using UnityEngine;

namespace World
{
    public class RoomDoor : MonoBehaviour
    {
        public event Action OnPlayerEnter;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                OnPlayerEnter?.Invoke();
        }
    }
}