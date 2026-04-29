using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Implements IDebuffable (write) and IDebuffReadable (read).
    /// One component no the enemy GO handles all active debuff types simultaneously.
    /// EnemyAI reads via IDebuffReadable - never needs to know about Core.
    /// </summary>
    public sealed class DebuffComponent : MonoBehaviour, IDebuffable, IDebuffReadable
    {
        private readonly Dictionary<DebuffType, DebuffEntry> _active = new();
        private Coroutine _tickRoutine;
        
        // ── Lifetime registration ────────────────────────────────────────────────

        private void OnEnable()
        {
            // Notify the enemy that a debuff component is now active.
            // EnemyAI (or handler) caches this reference — no GetComponent needed.
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
        }
        
        // ── IDebuffable ──────────────────────────────────────────────────────────
        public void ApplyDebuff(DebuffType type, float strength, float duration)
        {
            // Refresh duration and strength — does not stack, mirrors DoTComponent
            _active[type] = new DebuffEntry { Strength = strength, Remaining = duration };

            if (_tickRoutine == null)
                _tickRoutine = StartCoroutine(TickRoutine());
        }
        
        // ── IDebuffReadable ──────────────────────────────────────────────────────

        public float GetDebuffStrength(DebuffType type)
        {
            return _active.TryGetValue(type, out var entry) ? entry.Strength : 0f;
        }

        public bool IsDebuffed(DebuffType type) => _active.ContainsKey(type);

        // ── Internal tick ────────────────────────────────────────────────────────

        private IEnumerator TickRoutine()
        {
            while (_active.Count > 0)
            {
                yield return Helpers.GetWait(0.1f);

                // Tick all entries — collect expired types to remove after iteration
                var toRemove = new List<DebuffType>();
                foreach (var type in new List<DebuffType>(_active.Keys))
                {
                    var entry = _active[type];
                    entry.Remaining -= 0.1f;

                    if (entry.Remaining <= 0f)
                        toRemove.Add(type);
                    else
                        _active[type] = entry;
                }

                foreach (var type in toRemove)
                    _active.Remove(type);
            }

            _tickRoutine = null;
            Destroy(this);
        }
    }
}