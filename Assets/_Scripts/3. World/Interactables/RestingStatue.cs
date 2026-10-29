using System;
using Core;
using DG.Tweening;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    public abstract class RestingStatue : MonoBehaviour
    {
        public event Action OnStatueInteracted;
        
        [Header("Settings")]
        [SerializeField] private GameObject _activeVFX;
        [SerializeField] private GameObject _interactionPrompt;
        [SerializeField] private float _interactionRadius = 2.5f;
        [SerializeField] private float _promptFadeDuration = 0.2f;
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _interactSound;

        private bool _isActive = true;
        private bool _playerInside = false;
        private GameObject _cachedPlayer;

        private Image _promptSprite;
        private Tween _promptTween;

        private void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _interactionRadius;
            
            if (_interactionPrompt != null)
            {
                _promptSprite = _interactionPrompt.GetComponentInChildren<Image>();
                _interactionPrompt.SetActive(false);
                
                if (_promptSprite != null)
                {
                    var color = _promptSprite.color;
                    color.a = 0f;
                    _promptSprite.color = color;
                }
            }
        }

        private void OnDisable()
        {
            if (_playerInside && Helpers.Input != null)
            {
                Helpers.Input.OnInteractPressed -= HandleInteractPressed;
            }
            
            _promptTween?.Kill();
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

                ShowPrompt();
                
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

                HidePrompt();

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

            HidePrompt();

            if (Helpers.Input != null)
            {
                Helpers.Input.OnInteractPressed -= HandleInteractPressed;
            }
        }

        private void ShowPrompt()
        {
            if (_interactionPrompt == null || _promptSprite == null) return;
            
            _interactionPrompt.SetActive(true);
            _promptTween?.Kill();
            _promptTween = _promptSprite.DOFade(1f, _promptFadeDuration);
        }

        private void HidePrompt()
        {
            if (_interactionPrompt == null || _promptSprite == null) return;

            _promptTween?.Kill();
            _promptTween = _promptSprite.DOFade(0f, _promptFadeDuration)
                .OnComplete(() => _interactionPrompt.SetActive(false));
        }
        
        protected abstract void ApplyReward(GameObject player);
    }
}