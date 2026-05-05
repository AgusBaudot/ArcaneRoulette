using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Foundation
{
    public class ProjectilePrefabFactory : MonoBehaviour
    {
        public static ProjectilePrefabFactory Instance { get; private set; }

        [SerializeField] private Transform _poolRoot;

        private readonly Dictionary<int, object> _pools = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject); 
                return;
            }
            
            Instance = this;
            if (_poolRoot == null) 
                _poolRoot = transform;
        }

        /// <summary>
        /// Spawns an instance of the prefab. Creates a new pool if one doesn't exist.
        /// </summary>
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component, IPoolable
        {
            int prefabId = prefab.gameObject.GetInstanceID();

            if (!_pools.TryGetValue(prefabId, out var poolObj))
            {
                poolObj = CreatePool(prefab);
                _pools[prefabId] = poolObj;
            }

            var pool = (ObjectPool<T>)poolObj;
            T instance = pool.Get();

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.OnSpawn(); // Triggers interface

            return instance;
        }

        /// <summary>
        /// Global entry point to despawn any pooled object.
        /// </summary>
        public void Despawn(GameObject instanceObj)
        {
            if (instanceObj.TryGetComponent<PoolLink>(out var link))
            {
                link.Release();
            }
            else
            {
                Debug.LogWarning($"[PrefabFactory] Attempted to despawn an unpooled object: {instanceObj.name}");
                Destroy(instanceObj);
            }
        }

        private ObjectPool<T> CreatePool<T>(T prefab) where T : Component, IPoolable
        {
            ObjectPool<T> pool = null;

            pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    T instance = Instantiate(prefab, _poolRoot);
                    
                    // Attach the link so the instance knows how to return home
                    var link = instance.gameObject.AddComponent<PoolLink>();
                    link.Init(() =>
                    {
                        instance.OnDespawn(); // Triggers interface
                        pool.Release(instance);
                    });
                    
                    return instance;
                },
                actionOnGet: (instance) => instance.gameObject.SetActive(true),
                actionOnRelease: (instance) => instance.gameObject.SetActive(false),
                actionOnDestroy: (instance) => Destroy(instance.gameObject),
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 500 // Prevents runaway memory leaks if a bug causes infinite spawning
            );

            return pool;
        }
    }

    /// <summary>
    /// Internal Foundation component. Keeps Core/World bands gracefully unaware of UnityEngine.Pool.
    /// </summary>
    [DisallowMultipleComponent]
    internal class PoolLink : MonoBehaviour
    {
        private Action _releaseAction;
        public void Init(Action releaseAction) => _releaseAction = releaseAction;
        public void Release() => _releaseAction?.Invoke();
    }
}