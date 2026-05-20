using System;
using UnityEngine;

namespace World
{
    public class HardcoreDoor : MonoBehaviour
    {
        public event Action OnPlayerEnter;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            if (!other.isTrigger)
                return;

            OnPlayerEnter?.Invoke();
        }
    } 
}
