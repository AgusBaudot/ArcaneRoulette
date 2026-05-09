using System;
using UnityEngine;

namespace World
{
    public enum EdgeDirection 
    {
        up, down, left, right
    }
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] private EdgeDirection _direction;
        public event Action<EdgeDirection, Collider> OnPlayerEnter;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            OnPlayerEnter?.Invoke(_direction, other);
        }
    }
}