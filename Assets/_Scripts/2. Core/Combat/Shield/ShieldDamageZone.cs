using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    public sealed class ShieldDamageZone : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Player;
        
        [SerializeField] private float _tickInterval = 0.3f;
        [SerializeField] private int _damagePerTick = 2;
        
        private readonly HashSet<IDamageable> _targetsInZone = new();
        private float _timeSinceLastTick;
        private ElementType _currentElement = ElementType.Neutral;
        
        public bool Active { get; set; }

        public void Bind(ElementType element)
        {
            _currentElement = element;
        }

        private void OnEnable()
        {
            UpdateManager.Instance?.Register(this);
            _targetsInZone.Clear();
            _timeSinceLastTick = 0f;
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            _targetsInZone.Clear();
            Active = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Active) return;
            
            if (other.GetComponentInParent<PlayerController>() != null) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                _targetsInZone.Add(damageable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                _targetsInZone.Remove(damageable);
            }
        }

        public void Tick(float dt)
        {
            if (!Active || _targetsInZone.Count == 0) return;

            _timeSinceLastTick += dt;
            if (_timeSinceLastTick >= _tickInterval)
            {
                _timeSinceLastTick -= _tickInterval;
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            var batch = new DamageBatch();

            foreach (var target in _targetsInZone)
            {
                if (target == null) continue;
                
                batch.Deal(target, ((Component)target).gameObject, _damagePerTick, _currentElement);
            }

            batch.Commit(Helpers.Combat.SmallDMG);
        }
    }
}