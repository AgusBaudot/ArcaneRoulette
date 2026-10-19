using Core;
using DG.Tweening;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    [RequireComponent(typeof(Collider))]
    public sealed class RewardChest : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField] private float _fallDuration = 0.4f;
        [SerializeField] private float _promptFadeDuration = 0.2f;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _interactionPrompt;
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _fallSound;
        [SerializeField] private AudioEventSO _openSound;
        [SerializeField] private AudioEventSO _interactSound;
        
        private bool _isOpen;
        private bool _playerInRange;
        
        private Image _promptSprite;
        private Tween _promptTween;
        
        private static readonly int _openHash = Animator.StringToHash("t_Open");

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
            
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

        private void OnEnable()
        {
            //TODO: make fall
            
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, _fallDuration).SetEase(Ease.OutBack);
        }

        private void OnDisable()
        {
            Helpers.Input.OnInteractPressed -= HandleInteract;
            _promptTween?.Kill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isOpen || _playerInRange)
                return;
            
            var playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                _playerInRange = true;
                Helpers.Input.OnInteractPressed += HandleInteract;

                ShowPrompt();
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
                Helpers.Input.OnInteractPressed -= HandleInteract;

                HidePrompt();
            }
        }

        private void HandleInteract()
        {
            if (!_playerInRange || _isOpen)
                return;

            _isOpen = true;
            Helpers.Input.OnInteractPressed -= HandleInteract;
            
            HidePrompt();

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
    }
}