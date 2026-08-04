using System;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class HealingPatch : MonoBehaviour
    {
        [SerializeField] [Range(0, 1)] private float _healAmount = 0.5f; 
        [SerializeField] private float _radius = 2f;

        private bool _playerInside;
        private PlayerHealth _playerHealth;

        private void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _radius * 0.25f;
            transform.GetChild(0).gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Helpers.Input.OnInteractPressed += HandleInteraction;
        }

        private void OnDisable()
        {
            if (Helpers.Input != null)
                Helpers.Input.OnInteractPressed -= HandleInteraction;
        }

        private void HandleInteraction()
        {
            if (!_playerInside || _playerHealth == null)
                return;
            
            _playerHealth.Heal(Mathf.RoundToInt(GameStateManager.RunState.MaxHp * _healAmount));

            var room = GetComponentInParent<RoomManager>();
            if (room != null)
            {
                room.MarkAsCleared();
            }
            
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerHealth = other.GetComponentInParent<PlayerHealth>();
                
                transform.GetChild(0).gameObject.SetActive(true);
                _playerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerHealth = null; // Clear the cache
                transform.GetChild(0).gameObject.SetActive(false);
                _playerInside = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, 0, transform.position.z), _radius);
        }
    }
}