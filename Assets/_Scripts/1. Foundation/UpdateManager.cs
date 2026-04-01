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
            if (_updatables.Contains(updatable))
                return;
            _updatables.Add(updatable);
            _updatablesDirty = true;
        }
        
        public void Unregister(IUpdatable updatable)
            => _updatables.Remove(updatable);

        public void Register(IFixedUpdatable fixedUpdatable)
        {
            if (_fixedUpdatables.Contains(fixedUpdatable))
                return;
            _fixedUpdatables.Add(fixedUpdatable);
            _fixedUpdatablesDirty = true;
        }

        public void Unregister(IFixedUpdatable fixedUpdatable)
            => _fixedUpdatables.Remove(fixedUpdatable);
        
        // ── Unity loop ────────────────────────────────────────────────────
        private void Update()
        {
            if (_updatablesDirty)
            {
                _updatables.Sort((a, b) => a.UpdatePriority.CompareTo(b.UpdatePriority));
                _updatablesDirty = false;
            }
            
            //Snapshot count - registration during Tick are deferred to the next frame.
            foreach (var updatable in _updatables)
                updatable.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_fixedUpdatablesDirty)
            {
                _updatables.Sort((a, b) => a.UpdatePriority.CompareTo(b.UpdatePriority));
                _fixedUpdatablesDirty = false;
            }

            foreach (var fixedUpdatable in _fixedUpdatables)
                fixedUpdatable.FixedTick(Time.fixedDeltaTime);
        }
    }
}