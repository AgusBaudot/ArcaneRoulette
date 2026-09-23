using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Foundation;
using UnityEngine.AI;

namespace World 
{
    public class PoolEnemy : MonoBehaviour
    {
        [SerializeField] private List<PoolConfig> _poolConfigs;
        private Dictionary<EnemyType, ObjectPool<IPoolable>> _pools;

        public static PoolEnemy Instance { get; private set; }
        private bool _isReady = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate PoolEnemy on '{name}' — destroying it, keeping '{Instance.name}'.", this);
                Destroy(this);
                return;
            }
            Instance = this;
            _pools = new Dictionary<EnemyType, ObjectPool<IPoolable>>();
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var poolConfig in _poolConfigs)
            {
                var prefab = poolConfig._enemy;
                var pool = new ObjectPool<IPoolable>(
                    createFunc: () =>
                    {
                        // 1. Parent to the manager to prevent scene root clutter
                        var obj = Instantiate(prefab, transform);

                        // 2. Disable agent immediately to prevent origin-snap errors
                        if (obj.TryGetComponent<NavMeshAgent>(out var agent))
                            agent.enabled = false;
                        
                        // 3. Keep inactive so they don't flash on screen during instantiation
                        obj.SetActive(false);

                        return obj.GetComponent<EnemyController>();
                    },
                    actionOnGet: obj => 
                    { 
                        // Empty! We handle this manually in Get() to strictly control the order of operations.
                    },
                    actionOnRelease: obj => 
                    {
                        obj.OnDespawn();
                        // Guarantee the object disappears, backing up EnemyController's own deactivation
                        ((MonoBehaviour)obj).gameObject.SetActive(false);
                    },
                    actionOnDestroy: obj => Destroy(((MonoBehaviour)obj).gameObject), 
                    collectionCheck: true, 
                    defaultCapacity: poolConfig._initialSize, 
                    maxSize: poolConfig._maxSize
                );
                
                _pools.Add(poolConfig._id, pool);

                Queue<IPoolable> tempQueue = new Queue<IPoolable>();
                for (int i = 0; i < poolConfig._initialSize; i++)
                {
                    var entity = pool.Get();
                    var mono = (MonoBehaviour)entity;
                    
                    // Manually simulate a safe lifecycle for warmup
                    mono.transform.position = transform.position; 
                    mono.gameObject.SetActive(true);
                    entity.OnSpawn();
                    
                    tempQueue.Enqueue(entity);
                }
                
                while (tempQueue.Count > 0)
                {
                    // Release triggers actionOnRelease -> OnDespawn() & SetActive(false)
                    pool.Release(tempQueue.Dequeue());
                }
            }
            _isReady = true;
        }

        public IPoolable Get(EnemyType enemyType, Vector3 position)
        {
            if (!_isReady) return null;

            if (!_pools.TryGetValue(enemyType, out var pool))
            {
                Debug.LogError($"PoolEnemy: no PoolConfig registered for {enemyType}.");
                return null;
            }

            var entity = pool.Get() as EnemyController;
            if (entity == null)
            {
                Debug.LogError($"PoolEnemy: pooled object for {enemyType} has no EnemyController component.");
                return null;
            }

            entity.transform.position = position; 
            
            entity.gameObject.SetActive(true);    
            
            entity.OnSpawn();                     

            return entity;
        }

        public void Release(EnemyType enemyType, IPoolable obj)
        {
            if (!_pools.TryGetValue(enemyType, out var pool))
            {
                Debug.LogError($"PoolEnemy: no PoolConfig registered for {enemyType} — {((MonoBehaviour)obj).name} can't be released and will leak.");
                return;
            }
            pool.Release(obj);
        } 
    }

    [System.Serializable]
    public class PoolConfig 
    {
        public EnemyType _id;
        public GameObject _enemy;
        public int _initialSize;
        public int _maxSize;
    }
}