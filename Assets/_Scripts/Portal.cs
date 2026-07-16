using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public enum PortalType { NextFloor, StartNewRun }

    [RequireComponent(typeof(SphereCollider))]
    public class Portal : MonoBehaviour
    {
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private PortalType _type = PortalType.NextFloor;

        private string _targetSceneName = SceneNames.GameLevel;
        private bool _hasTriggered = false;

        private void Start()
        {
            var col = GetComponent<SphereCollider>();
            col.radius = _radius;
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered) return;

            if (other.TryGetComponent<PlayerHurtBox>(out _))
            {
                _hasTriggered = true;

                if (_type == PortalType.NextFloor)
                {
                    EventBus.Publish(new FloorClearedEvent());
                }
                else if (_type == PortalType.StartNewRun)
                {
                    EventBus.Publish(new EndRunRequestEvent(_targetSceneName));
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _type == PortalType.NextFloor ? Color.cyan : Color.green;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}