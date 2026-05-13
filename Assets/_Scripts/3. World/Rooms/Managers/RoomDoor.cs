using System;
using UnityEngine;

namespace World
{
    public enum EdgeDirection 
    {
        Up, Down, Left, Right
    }
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] private EdgeDirection _direction;
        public event Action<EdgeDirection> OnPlayerEnter;
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            OnPlayerEnter?.Invoke(_direction);
        }
    }
}