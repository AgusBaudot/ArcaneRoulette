using UnityEngine;
using Foundation;

namespace World
{
    public enum PortalType { NextFloor, StartNewRun }

    [RequireComponent(typeof(SphereCollider))]
    public class TransitionPortal : MonoBehaviour
    {
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private PortalType _type = PortalType.NextFloor;
        [SerializeField] private GameObject _keyIcon;

        private readonly string _targetSceneName = SceneNames.GameLevel;
        private bool _hasTriggered;
        private bool _playerInside;

        private void Start()
        {
            var col = GetComponent<SphereCollider>();
            col.radius = _radius;
            col.isTrigger = true;

            Helpers.Input.OnInteractPressed += HandleInteraction;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _keyIcon.SetActive(true);
                _playerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _keyIcon.SetActive(false);
                _playerInside = false;
            }
        }

        private void HandleInteraction()
        {
            if (!_playerInside || _hasTriggered)
                return;

            _hasTriggered = true;

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

            Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _type == PortalType.NextFloor ? Color.cyan : Color.green;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}