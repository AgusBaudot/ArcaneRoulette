using System;
using Core;
using DG.Tweening;
using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Collider))]
    public sealed class RewardChest : MonoBehaviour
    {
        //Add E sprite from statues
        //Make chest the actual FBX
        
        [Header("General Settings")]
        [SerializeField] private float _fallDuration = 0.4f;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _interactionPrompt;
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _fallSound;
        [SerializeField] private AudioEventSO _openSound;
        [SerializeField] private AudioEventSO _interactSound;
        
        private bool _isOpen;
        private bool _playerInRange;
        
        private static readonly int _openHash = Animator.StringToHash("t_Open");

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
            
            if (_interactionPrompt != null)
                _interactionPrompt.SetActive(false);
        }

        private void OnEnable()
        {
            //TODO: make fall
            
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, _fallDuration).SetEase(Ease.OutBack);
        }

        private void OnDisable()
        {
            Helpers.Input.OnInteractPressed -= HandleInteract;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isOpen || _playerInRange)
                return;
            
            var playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                _playerInRange = true;

                if (_interactionPrompt != null)
                {
                    _interactionPrompt.SetActive(true);
                }
                
                Helpers.Input.OnInteractPressed += HandleInteract;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isOpen || !_playerInRange)
                return;
            
            var playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                _playerInRange = false;

                if (_interactionPrompt != null)
                {
                    _interactionPrompt.SetActive(false);
                }

                Helpers.Input.OnInteractPressed -= HandleInteract;
            }
        }

        private void HandleInteract()
        {
            if (!_playerInRange || _isOpen)
                return;

            _isOpen = true;

            if (_animator != null)
            {
                _animator.SetTrigger(_openHash);
            }
            else
            {
                Debug.LogWarning("Chest open animation missing.");
            }

            if (_openSound != null)
            {
                EventBus.Publish(new AudioPlayRequest
                {
                    Event = _openSound,
                    WorldPosition = transform.position
                });
            }
            
            EventBus.Publish(new LootChestOpenedEvent(transform.position));
        }
    }
}