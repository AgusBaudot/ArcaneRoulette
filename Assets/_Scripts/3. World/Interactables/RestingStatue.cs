using System;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public abstract class RestingStatue : MonoBehaviour
    {
        public event Action OnStatueInteracted;
        
        [Header("Settings")]
        [SerializeField] private GameObject _activeVFX;
        [SerializeField] private GameObject _interactionPrompt;
        [SerializeField] private float _interactionRadius = 2.5f;
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _interactSound;

        private bool _isActive = true;
        private bool _playerInside = false;
        private GameObject _cachedPlayer;

        private void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _interactionRadius;
            
            if (_interactionPrompt != null)
                _interactionPrompt.SetActive(false);
        }

        private void OnDisable()
        {
            if (_playerInside && Helpers.Input != null)
            {
                Helpers.Input.OnInteractPressed -= HandleInteractPressed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _playerInside)
                return;
            
            var playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                _playerInside = true;
                _cachedPlayer = playerController.gameObject;

                if (_interactionPrompt != null)
                {
                    _interactionPrompt.SetActive(true);
                }
                
                Helpers.Input.OnInteractPressed += HandleInteractPressed;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_isActive || !_playerInside)
                return;
            
            var playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null && playerController.gameObject == _cachedPlayer)
            {
                _playerInside = false;
                _cachedPlayer = null;

                if (_interactionPrompt != null)
                {
                    _interactionPrompt.SetActive(false);
                }

                Helpers.Input.OnInteractPressed -= HandleInteractPressed;
            }
        }

        private void HandleInteractPressed()
        {
            if (!_isActive || !_playerInside || _cachedPlayer == null)
                return;

            if (_interactSound != null)
            {
                EventBus.Publish(new AudioPlayRequest
                {
                    Event = _interactSound,
                    WorldPosition = transform.position
                });
            }

            ApplyReward(_cachedPlayer);
            OnStatueInteracted?.Invoke();
        }

        public void Deactivate()
        {
            _isActive = false;
            _playerInside = false;

            if (_activeVFX != null)
            {
                _activeVFX.SetActive(false);
            }

            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(false);
            }

            if (Helpers.Input != null)
            {
                Helpers.Input.OnInteractPressed -= HandleInteractPressed;
            }
        }
        
        protected abstract void ApplyReward(GameObject player);
    }
}