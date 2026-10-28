using Core;
using DG.Tweening;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    [RequireComponent(typeof(Collider))]
    public class TutorialRewardChest : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _fallDuration = 0.4f;
        [SerializeField] private float _promptFadeDuration = 0.4f;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _interactionPrompt;
        [SerializeField] private AudioEventSO _fallSound;
        [SerializeField] private AudioEventSO _openSound;

        private bool _isOpen;
        private bool _playerInRange;
        private Image _promptSprite;
        private Tween _promptTween;
        
        private static readonly int _openHash = Animator.StringToHash("t_Open");
        
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (_interactionPrompt != null)
            {
                _promptSprite = _interactionPrompt.GetComponentInChildren<Image>();
                _interactionPrompt.SetActive(false);
                var color = _promptSprite.color; color.a = 0f; _promptSprite.color = color;
            }
        }

        private void OnEnable()
        {
            if (_fallSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = _fallSound, WorldPosition = transform.position });
            
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
            if (_isOpen || _playerInRange) return;
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = true;
                Helpers.Input.OnInteractPressed += HandleInteract;
                ShowPrompt();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isOpen || !_playerInRange) return;
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = false;
                Helpers.Input.OnInteractPressed -= HandleInteract;
                HidePrompt();
            }
        }

        private void HandleInteract()
        {
            if (!_playerInRange || _isOpen) return;

            _isOpen = true;
            Helpers.Input.OnInteractPressed -= HandleInteract;
            HidePrompt();

            if (_animator != null) _animator.SetTrigger(_openHash);
            if (_openSound != null) EventBus.Publish(new AudioPlayRequest { Event = _openSound, WorldPosition = transform.position });

            EventBus.Publish(new TutorialChestOpenedEvent(transform.position));
        }

        private void ShowPrompt()
        {
            _interactionPrompt.SetActive(true);
            _promptTween?.Kill();
            _promptTween = _promptSprite.DOFade(1f, _promptFadeDuration);
        }

        private void HidePrompt()
        {
            _promptTween?.Kill();
            _promptTween = _promptSprite.DOFade(0f, _promptFadeDuration)
                .OnComplete(() => _interactionPrompt.SetActive(false));
        }
    }
}