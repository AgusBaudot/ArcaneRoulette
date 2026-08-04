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
            Instance = this;
            _pools = new Dictionary<EnemyType, ObjectPool<IPoolable>>();
        }
        private void Start()
        {
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

            _pools.TryGetValue(enemyType, out var pool);
            var entity = pool.Get() as EnemyController;
            
            entity.Transform.position = position;
            
            return entity;
        }
        public void Release(EnemyType enemyType, IPoolable obj)
        {
            _pools.TryGetValue(enemyType, out var pool);
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
