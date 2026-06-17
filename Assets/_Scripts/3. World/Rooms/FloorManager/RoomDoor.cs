using System;
using UnityEngine;

namespace World
{
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] private EdgeDirection _direction;
        public event Action<EdgeDirection> OnPlayerEnter;

        private float _lastTriggerTime;
        private const float TRIGGER_COOLDOWN = 0.25f;

        private void OnEnable()
        {
            _lastTriggerTime = Time.time;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (Time.time < _lastTriggerTime + TRIGGER_COOLDOWN)
                return;

            _lastTriggerTime = Time.time;
            OnPlayerEnter?.Invoke(_direction);
        }
    }
}