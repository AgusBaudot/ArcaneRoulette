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
        [SerializeField] private string _targetSceneName = "Core loop";

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

            // Perfectly respects your conventions: PlayerHurtBox is the physical trigger
            if (other.TryGetComponent<PlayerHurtBox>(out _))
            {
                _hasTriggered = true;

                if (_type == PortalType.NextFloor)
                {
                    // Aligned with your defined EventBus convention structs
                    EventBus.Publish(new EndFloorClearEvent());
                }
                else if (_type == PortalType.StartNewRun)
                {
                    // Delegate the teardown and scene load securely to Foundation
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