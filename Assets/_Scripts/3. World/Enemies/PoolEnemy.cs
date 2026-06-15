using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Foundation;

namespace World 
{
    public class PoolEnemy : MonoBehaviour
    {
        [SerializeField] private List<PoolConfig> _poolConfigs;
        private Dictionary<EnemyType, ObjectPool<IPoolable>> _pools;

        public static PoolEnemy Instance { get; private set; }
        private void Awake()
        {
            Instance = this;
            _pools = new Dictionary<EnemyType, ObjectPool<IPoolable>>();

            foreach (var poolConfig in _poolConfigs)
            {
                var prefab = poolConfig._enemy;
                var pool = new ObjectPool<IPoolable>(
                    () =>
                    {
                        var obj = Instantiate(prefab);
                        //var pooleable = obj.GetComponent<IPoolable>();
                        var pooleable = obj.GetComponent<EnemyController>();
                        return pooleable;
                    },
                    obj => obj.OnSpawn(),
                    obj => obj.OnDespawn(), 
                    obj => Destroy(((MonoBehaviour)obj).gameObject), true, poolConfig._initialSize, poolConfig._maxSize );
                _pools.Add(poolConfig._id, pool);
            }
        }
        public IPoolable Get(EnemyType enemyType, Vector3 position)
        {
            _pools.TryGetValue(enemyType, out var pool);
            var entity = pool.Get() as EnemyController;
            entity.Transform.position = position;
            //entity.GetComponent<NavMeshAgent>().Warp(position);
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
