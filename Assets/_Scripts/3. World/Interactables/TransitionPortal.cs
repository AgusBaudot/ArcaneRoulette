using DG.Tweening;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    public enum PortalType { NextFloor, StartNewRun, ReturnToLobby }

    [RequireComponent(typeof(SphereCollider))]
    public class TransitionPortal : MonoBehaviour
    {
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private PortalType _type = PortalType.NextFloor;
        [SerializeField] private GameObject _keyIcon;
        [SerializeField] private float _iconFadeDuration = 0.2f;

        private readonly string _targetSceneName = SceneNames.GameLevel;
        private bool _hasTriggered;
        private bool _playerInside;

        private Image _iconSprite;
        private Tween _iconTween;

        private void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.radius = _radius;
            col.isTrigger = true;

            if (_keyIcon != null)
            {
                _iconSprite = _keyIcon.GetComponentInChildren<Image>();
                _keyIcon.SetActive(false);
                
                if (_iconSprite != null)
                {
                    var color = _iconSprite.color;
                    color.a = 0f;
                    _iconSprite.color = color;
                }
            }
        }

        private void OnEnable()
        {
            Helpers.Input.OnInteractPressed += HandleInteraction;
        }

        private void OnDisable()
        {
            Helpers.Input.OnInteractPressed -= HandleInteraction;
            _iconTween?.Kill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInside = true;
                ShowIcon();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInside = false;
                HideIcon();
            }
        }

        private void HandleInteraction()
        {
            if (!_playerInside || _hasTriggered)
                return;

            _hasTriggered = true;
            HideIcon();

            if (_type == PortalType.NextFloor)
            {
                if (GameStateManager.RunState != null && GameStateManager.RunState.CurrentFloor >= 8)
                {
                    EventBus.Publish(new EndRunRequestEvent(SceneNames.MainMenu));
                }
                else
                {
                    EventBus.Publish(new FloorClearedEvent());
                }
            }
            else if (_type == PortalType.StartNewRun)
            {
                EventBus.Publish(new StartRunRequestEvent(_targetSceneName));
            }
            else if (_type == PortalType.ReturnToLobby)
            {
                EventBus.Publish(new EndRunRequestEvent(SceneNames.Lobby));
            }

            Destroy(gameObject);
        }

        private void ShowIcon()
        {
            if (_keyIcon == null || _iconSprite == null) return;
            
            _keyIcon.SetActive(true);
            _iconTween?.Kill();
            _iconTween = _iconSprite.DOFade(1f, _iconFadeDuration);
        }

        private void HideIcon()
        {
            if (_keyIcon == null || _iconSprite == null) return;

            _iconTween?.Kill();
            _iconTween = _iconSprite.DOFade(0f, _iconFadeDuration)
                .OnComplete(() => _keyIcon.SetActive(false));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _type == PortalType.NextFloor ? Color.cyan : 
                           _type == PortalType.StartNewRun ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}