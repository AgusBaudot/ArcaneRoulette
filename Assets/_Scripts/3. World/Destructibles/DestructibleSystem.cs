using UnityEngine;
using Foundation;

namespace World
{
    /// <summary>
    /// Attachable component that turns any prefab into a destructible prop.
    /// Put this on a GameObject that has (or gets, via fallback below) a trigger
    /// Collider acting as its hitbox — same layer as your regular wall geometry.
    /// A destructible IS a wall physically; the only thing that changes is that
    /// BaseProjectile.OnHitWall also finds an IDestructible on it.
    ///
    /// Starts Unbroken. OnDeath() flips to Destroyed exactly once, disables the
    /// hitbox, fires the break animation + sound, and rolls the crystal drop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DestructibleSystem : MonoBehaviour, IDestructible
    {
        [Header("Hitbox")]
        [Tooltip("Disabled the instant this breaks, so it stops registering hits " +
                 "or blocking movement while the destroy animation plays. Falls " +
                 "back to GetComponent<Collider>() if left empty.")]
        [SerializeField] private Collider _hitboxCollider; //Trigger collider for collisions.
        [SerializeField] private Collider _hardCollider; //Non-trigger collider for "wall-like" pre-destruction.

        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _destroyedTrigger = "Destroyed";

        [Header("Feedback")]
        [SerializeField] private AudioEventSO _breakSound;

        [Header("Drop Table")]
        [Tooltip("Chance [0-1] to drop a crystal on destruction. Default 0.4 = 40%.")]
        [SerializeField, Range(0f, 1f)] private float _crystalDropChance = 0.4f;
        [Tooltip("CurrencyDrop always grants exactly 1 currency (Collect() hardcodes " +
                 "AddCurrency(1)), so there's no amount field here to configure.")]
        [SerializeField] private CurrencyDrop _currencyDropPrefab;

        private int _destroyedTriggerHash;

        public DestructibleState State { get; private set; } = DestructibleState.Unbroken;
        public bool IsDestroyed => State == DestructibleState.Destroyed;

        private void Awake()
        {
            State = DestructibleState.Unbroken;
            _destroyedTriggerHash = Animator.StringToHash(_destroyedTrigger);

            if (_hitboxCollider == null)
            {
                _hitboxCollider = GetComponent<Collider>();
            }
            
            if (_hitboxCollider != null)
            {
                _hitboxCollider.isTrigger = true;
            }
 
            if (_hardCollider != null)
            {
                _hardCollider.isTrigger = false;
            }

#if UNITY_EDITOR
            if (_hitboxCollider == null)
            {
                Debug.LogWarning($"{nameof(DestructibleSystem)} on '{name}' has no hitbox Collider — it can never register a death.", this);
            }
            if (_animator == null)
            {
                Debug.LogWarning($"{nameof(DestructibleSystem)} on '{name}' has no Animator assigned — destroy animation will be skipped.", this);
            }
            if (_currencyDropPrefab == null)
            {
                Debug.LogWarning($"{nameof(DestructibleSystem)} on '{name}' has no CurrencyDrop prefab assigned — the crystal roll will silently do nothing.", this);
            }
#endif
        }

        public void OnDeath(Vector3 hitPosition)
        {
            if (State == DestructibleState.Destroyed)
            {
                return;
            }

            State = DestructibleState.Destroyed;

            if (_hitboxCollider != null)
            {
                _hitboxCollider.enabled = false;
            }

            if (_hardCollider != null)
            {
                _hardCollider.enabled = false;
            }

            if (_animator != null)
            {
                _animator.SetTrigger(_destroyedTriggerHash);
            }

            if (_breakSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _breakSound, WorldPosition = hitPosition });
            }

            RollDropTable(hitPosition);
        }

        private void RollDropTable(Vector3 dropPosition)
        {
            if (Random.value >= _crystalDropChance)
            {
                return; // 60% default outcome — nothing drops.
            }

            if (_currencyDropPrefab == null)
            {
                return;
            }

            var drop = Helpers.ProjFactory.Spawn<CurrencyDrop>(_currencyDropPrefab, dropPosition, Quaternion.identity);
            drop.InitDrop(dropPosition);
        }
    }
}