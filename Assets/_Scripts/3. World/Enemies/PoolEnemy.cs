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
                    () =>
                    {
                        var obj = Instantiate(prefab);

                        if (obj.TryGetComponent<NavMeshAgent>(out var agent))
                            agent.enabled = false;

                        return obj.GetComponent<EnemyController>();
                    },
                    obj => obj.OnSpawn(),
                    obj => obj.OnDespawn(),
                    obj => Destroy(((MonoBehaviour)obj).gameObject), true, poolConfig._initialSize, poolConfig._maxSize);
                _pools.Add(poolConfig._id, pool);

                Queue<IPoolable> tempQueue = new Queue<IPoolable>();
                for (int i = 0; i < poolConfig._initialSize; i++)
                {
                    tempQueue.Enqueue(pool.Get());
                }
                while (tempQueue.Count > 0)
                {
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

            entity.Transform.position = position;
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