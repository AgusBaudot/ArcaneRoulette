using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public class PlayerHurtBox : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.85f;
        
        private PlayerHealth _health;

        private void Awake()
        {
            //Avoid misconfiguration in the prefab
            gameObject.layer = LayerMask.NameToLayer("PlayerHurtBox");
            GetComponent<Collider>().isTrigger = true;
        }

        public void Initialize(PlayerHealth health) => _health = health;


        public void TakeDamage(int amount, ElementType elementType)
        {
            _health.TakeDamage(amount, elementType);
            //Juice
            CameraShake.AddTrauma(_cameraTrauma);
            HitStop.Apply(_hitStopDuration);
        }
    }
}