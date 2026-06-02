using System;
using Core;
using UnityEngine;

namespace World
{
    public class HardcoreDoor : MonoBehaviour
    {
        public event Action OnPlayerEnter;

        [SerializeField] private GameObject _solidDoor;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerHurtBox>(out _))
                return;
            
            _solidDoor.SetActive(true);
            OnPlayerEnter?.Invoke();
            Destroy(gameObject);
        }
    } 
}
