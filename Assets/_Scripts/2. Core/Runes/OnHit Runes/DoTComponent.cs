using System;
using Foundation;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(IDamageable))]
    public sealed class DoTComponent : MonoBehaviour, IUpdatable, IDoTReadable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Spells;

        public event Action OnDoTApplied;
        public event Action OnDoTRemoved;
        
        public bool HasActiveDoTs => _activeDoTs.Count > 0;
        public IReadOnlyList<DoTInstance> ActiveDoTs => _activeDoTs;
        
        private readonly List<DoTInstance> _activeDoTs = new List<DoTInstance>();

        private IDamageable _damageable;
        
        private float _blinkTimer = 1f;
        private float _blinkDurationTimer = 0f;
        private const float BLINK_INTERVAL = 1f;

        private void Awake()
        {
            _damageable = GetComponent<IDamageable>();
        }

        private void OnEnable()
        {
            var receivers = GetComponents<IDoTReceiver>();
            foreach (var receiver in receivers)
            {
                receiver.RegisterDoT(this);
            }
            
            if (_activeDoTs.Count > 0 && UpdateManager.Instance != null)
            {
                UpdateManager.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            var receivers = GetComponents<IDoTReceiver>();
            foreach (var receiver in receivers)
            {
                receiver.UnregisterDoT();
            }
            
            ClearAll();
        }

        public void ClearAll()
        {
            bool hadActiveDoTs = _activeDoTs.Count > 0;
            
            if (UpdateManager.Instance != null)
            {
                UpdateManager.Instance.Unregister(this);
            }
            
            _activeDoTs.Clear();

            if (hadActiveDoTs)
            {
                OnDoTRemoved?.Invoke();
            }
        }

        public void AddDoT(DoTInstance instance)
        {
            bool wasEmpty = _activeDoTs.Count == 0;
            _activeDoTs.Add(instance);

            if (wasEmpty)
            {
                OnDoTApplied?.Invoke();
            }
            
            if (wasEmpty && enabled && UpdateManager.Instance != null)
            {
                UpdateManager.Instance.Register(this);
                _blinkTimer = BLINK_INTERVAL; 
            }
        }

        public void RemoveDoT(DoTInstance instance)
        {
            if (_activeDoTs.Remove(instance) && _activeDoTs.Count == 0)
            {
                OnDoTRemoved?.Invoke();
                ClearAll();
            }
        }

        public void Tick(float dt)
        {
            if (_activeDoTs.Count == 0) return;

            ProcessDamage(dt);
        }

        private void ProcessDamage(float dt)
        {
            var batch = new DamageBatch();
            bool hasRemoved = false;

            for (int i = _activeDoTs.Count - 1; i >= 0; i--)
            {
                if (i >= _activeDoTs.Count) continue;

                var dot = _activeDoTs[i];
                dot.RemainingDuration -= dt;
                dot.TimeUntilNextTick -= dt;

                if (dot.TimeUntilNextTick <= 0f)
                {
                    dot.TimeUntilNextTick += dot.TickInterval;
                    batch.Deal(_damageable, gameObject, dot.Damage, dot.Element);
            
                    if (_activeDoTs.Count == 0)
                    {
                        break;
                    }
                }

                if (dot.RemainingDuration <= 0f)
                {
                    if (i < _activeDoTs.Count && _activeDoTs[i] == dot)
                    {
                        _activeDoTs.RemoveAt(i);
                        hasRemoved = true;
                    }
                }
            }

            batch.Commit(DamageJuice.None);

            if (hasRemoved && _activeDoTs.Count == 0)
            {
                OnDoTRemoved?.Invoke();
                ClearAll();
            }
        }
    }
}