using System;
using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    public sealed class DebuffComponent : MonoBehaviour, IDebuffable, IDebuffReadable
    {
        public event Action<DebuffType> OnDebuffApplied; 
        public event Action<DebuffType> OnDebuffRemoved; 
        
        public IEnumerable<DebuffType> ActiveTypes => _active.Keys;

        private readonly Dictionary<DebuffType, List<DebuffEntry>> _active = new();
        private Coroutine _tickRoutine;

        private void OnEnable()
        {
            var receivers = GetComponents<IDebuffReceiver>();
            foreach (var receiver in receivers)
            {
                receiver.RegisterDebuff(this);
            }
        }

        private void OnDisable()
        {
            var receivers = GetComponents<IDebuffReceiver>();
            foreach (var receiver in receivers)
            {
                receiver.UnregisterDebuff();
            }
            if (_tickRoutine != null)
            {
                StopCoroutine(_tickRoutine);
                _tickRoutine = null;
            }
        }
        
        // Backwards compatibility with the existing IDebuffable interface
        public void ApplyDebuff(DebuffType type, float strength, float duration)
        {
            ApplyDebuff(type, strength, duration, "Default");
        }

        // Overload for source tracking and additive stacking
        public void ApplyDebuff(DebuffType type, float strength, float duration, string source)
        {
            if (!_active.ContainsKey(type))
            {
                _active[type] = new List<DebuffEntry>();
            }

            var list = _active[type];
            int existingIndex = list.FindIndex(e => e.Source == source);

            if (existingIndex >= 0)
            {
                // Struct requires a copy to modify
                var entry = list[existingIndex];
                entry.Strength = strength;
                entry.Remaining = duration;
                list[existingIndex] = entry;
            }
            else
            {
                list.Add(new DebuffEntry { Source = source, Strength = strength, Remaining = duration });
                
                if (list.Count == 1)
                {
                    OnDebuffApplied?.Invoke(type);
                }
            }

            if (_tickRoutine == null && gameObject.activeInHierarchy)
            {
                _tickRoutine = StartCoroutine(TickRoutine());
            }
        }
        
        public float GetDebuffStrength(DebuffType type)
        {
            if (!_active.TryGetValue(type, out var list)) return 0f;
            
            float totalStrength = 0f;
            foreach (var entry in list)
            {
                totalStrength += entry.Strength;
            }
            return totalStrength;
        }

        public bool IsDebuffed(DebuffType type) => _active.ContainsKey(type) && _active[type].Count > 0;

        private IEnumerator TickRoutine()
        {
            while (_active.Count > 0)
            {
                yield return CoroutineUtils.GetWait(0.1f);

                var emptyTypes = new List<DebuffType>();

                foreach (var type in new List<DebuffType>(_active.Keys))
                {
                    var list = _active[type];
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var entry = list[i];
                        entry.Remaining -= 0.1f;
                        
                        if (entry.Remaining <= 0f)
                        {
                            list.RemoveAt(i);
                        }
                        else
                        {
                            list[i] = entry;
                        }
                    }

                    if (list.Count == 0)
                    {
                        emptyTypes.Add(type);
                    }
                }

                foreach (var type in emptyTypes)
                {
                    _active.Remove(type);
                    OnDebuffRemoved?.Invoke(type);
                }
            }

            _tickRoutine = null;
        }
    }
}