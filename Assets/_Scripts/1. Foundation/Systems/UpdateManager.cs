using System.Collections.Generic;
using UnityEngine;


namespace Foundation
{
    /// <summary>
    /// Central update runner. All IUpdatable and IFixedUpdatable objects
    /// register here instead of using Unity's Update/FixedUpdate directly.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class UpdateManager : MonoBehaviour
    {
        public static UpdateManager Instance { get; private set; }

        //Separate lists - most objects only need on or the other.
        private readonly List<IUpdatable> _updatables = new();
        private readonly List<IFixedUpdatable> _fixedUpdatables = new();

        //Dirty flags - resort on next loop rather than on every Register call.
        private bool _updatablesDirty;
        private bool _fixedUpdatablesDirty;

        //Loop guards
        private bool _isUpdating;
        private bool _isFixedUpdating;
        private readonly List<IUpdatable> _pendingAddUpdatable = new();
        private readonly List<IUpdatable> _pendingRemoveUpdatable = new();
        private readonly List<IFixedUpdatable> _pendingAddFixed = new();
        private readonly List<IFixedUpdatable> _pendingRemoveFixed = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Registration ────────────────────────────────────────────────────
        public void Register(IUpdatable updatable)
        {
            if (_isUpdating)
            {
                if (!_pendingAddUpdatable.Contains(updatable))
                    _pendingAddUpdatable.Add(updatable);
                return;
            }
            if (_updatables.Contains(updatable)) return;
            _updatables.Add(updatable);
            _updatablesDirty = true;
        }

        public void Unregister(IUpdatable updatable)
        {
            if (_isUpdating)
            {
                if (!_pendingRemoveUpdatable.Contains(updatable))
                    _pendingRemoveUpdatable.Add(updatable);
                return;
            }
            _updatables.Remove(updatable);
        }

        public void Register(IFixedUpdatable fixedUpdatable)
        {
            if (_isFixedUpdating)
            {
                if (!_pendingAddFixed.Contains(fixedUpdatable))
                    _pendingAddFixed.Add(fixedUpdatable);
                return;
            }
            if (_fixedUpdatables.Contains(fixedUpdatable)) return;
            _fixedUpdatables.Add(fixedUpdatable);
            _fixedUpdatablesDirty = true;
        }

        public void Unregister(IFixedUpdatable fixedUpdatable)
        {
            if (_isFixedUpdating)
            {
                if (!_pendingRemoveFixed.Contains(fixedUpdatable))
                    _pendingRemoveFixed.Add(fixedUpdatable);
                return;
            }
            _fixedUpdatables.Remove(fixedUpdatable);
        }

        // ── Unity loop ────────────────────────────────────────────────────
        private void Update()
        {
            if (_updatablesDirty)
            {
                _updatables.Sort((a, b) => a.UpdatePriority.CompareTo(b.UpdatePriority));
                _updatablesDirty = false;
            }

            float dt = Time.deltaTime;
            _isUpdating = true;
            for (int i = 0; i < _updatables.Count; i++)
                _updatables[i].Tick(dt);
            _isUpdating = false;

            FlushPendingUpdatable();
        }

        private void FixedUpdate()
        {
            if (_fixedUpdatablesDirty)
            {
                _fixedUpdatables.Sort((a, b) =>
                    a.FixedUpdatePriority.CompareTo(b.FixedUpdatePriority));
                _fixedUpdatablesDirty = false;
            }
        
            float dt = Time.fixedDeltaTime;
            _isFixedUpdating = true;
            for (int i = 0; i < _fixedUpdatables.Count; i++)
                _fixedUpdatables[i].FixedTick(dt);
            _isFixedUpdating = false;
        
            FlushPendingFixed();
        }
        
        private void FlushPendingUpdatable()
        {
            foreach (var u in _pendingRemoveUpdatable) _updatables.Remove(u);
            _pendingRemoveUpdatable.Clear();

            foreach (var u in _pendingAddUpdatable)
            {
                if (_updatables.Contains(u)) continue;
                _updatables.Add(u);
                _updatablesDirty = true;
            }
            _pendingAddUpdatable.Clear();
        }
        
        private void FlushPendingFixed()
        {
            foreach (var u in _pendingRemoveFixed) _fixedUpdatables.Remove(u);
            _pendingRemoveFixed.Clear();

            foreach (var u in _pendingAddFixed)
            {
                if (_fixedUpdatables.Contains(u)) continue;
                _fixedUpdatables.Add(u);
                _fixedUpdatablesDirty = true;
            }
            _pendingAddFixed.Clear();
        }
    }
}