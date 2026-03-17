using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] private int _amount = 10;
        [SerializeField] private ElementType _element = ElementType.Neutral;
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.85f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<IDamageable>(out var damageable))
                return;
            
            damageable.TakeDamage(_amount, _element);
            //Juice
            CameraShake.AddTrauma(_cameraTrauma);
            HitStop.Apply(_hitStopDuration);
        }
    }
}