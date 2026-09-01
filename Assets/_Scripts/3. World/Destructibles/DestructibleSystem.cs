using UnityEngine;
using Foundation;

namespace World
{
    [DisallowMultipleComponent]
    public sealed class DestructibleSystem : MonoBehaviour, IDestructible
    {
        [Header("Hitbox")]
        [SerializeField] private Collider _hitboxCollider; 
        [SerializeField] private Collider _hardCollider; 

        [Header("Visual Swap")]
        [Tooltip("The intact model with 1 mesh (image_e35deb.png)")]
        [SerializeField] private GameObject _intactModel;
        
        [Tooltip("The fractured model containing the ExplodingObject script (image_e35e07.png)")]
        [SerializeField] private GameObject _fracturedModel;
        [SerializeField] private ExplodingObject _explosionController;

        [Header("Feedback")]
        [SerializeField] private AudioEventSO _breakSound;
        [SerializeField] private GameObject _explosionVFX;

        [Header("Drop Table")]
        [SerializeField, Range(0f, 1f)] private float _crystalDropChance = 0.4f;
        [SerializeField] private CurrencyDrop _currencyDropPrefab;

        public DestructibleState State { get; private set; } = DestructibleState.Unbroken;
        public bool IsDestroyed => State == DestructibleState.Destroyed;

        private void Awake()
        {
            State = DestructibleState.Unbroken;

            if (_hitboxCollider == null) _hitboxCollider = GetComponent<Collider>();
            if (_hitboxCollider != null) _hitboxCollider.isTrigger = true;
            if (_hardCollider != null) _hardCollider.isTrigger = false;

            // Ensure correct starting state
            if (_intactModel != null) _intactModel.SetActive(true);
            if (_fracturedModel != null) _fracturedModel.SetActive(false);
            if (_explosionVFX != null)
            {
                _explosionVFX.SetActive(false);
            }
        }

        public void OnDeath(Vector3 hitPosition)
        {
            if (State == DestructibleState.Destroyed) return;

            State = DestructibleState.Destroyed;

            if (_hitboxCollider != null) _hitboxCollider.enabled = false;
            if (_hardCollider != null) _hardCollider.enabled = false;

            // Perform the visual swap
            if (_intactModel != null) _intactModel.SetActive(false);
            
            if (_fracturedModel != null && _explosionController != null)
            {
                _fracturedModel.SetActive(true);
                _explosionVFX.gameObject.SetActive(true);
                _explosionController.TriggerExplosion(hitPosition);
            }

            if (_breakSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _breakSound, WorldPosition = hitPosition });
            }

            RollDropTable(hitPosition);
        }

        private void RollDropTable(Vector3 dropPosition)
        {
            if (Random.value >= _crystalDropChance) return; 
            if (_currencyDropPrefab == null) return;

            var drop = Helpers.ProjFactory.Spawn<CurrencyDrop>(_currencyDropPrefab, dropPosition, Quaternion.identity);
            drop.InitDrop(dropPosition);
        }
    }
}