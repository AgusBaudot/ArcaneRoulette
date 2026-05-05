using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace World 
{
    public enum EnemyType
    {
        melee, range, bruto
    }
    public class PoolEnemy : MonoBehaviour
    {
        [SerializeField] private List<PoolConfig> _poolConfigs;
        private Dictionary<EnemyType, ObjectPool<IPooleable>> _pools;
        private void Awake()
        {
            _pools = new Dictionary<EnemyType, ObjectPool<IPooleable>>();

            foreach (var poolConfig in _poolConfigs)
            {
                var prefab = poolConfig._enemy;
                var pool = new ObjectPool<IPooleable>(
                    () =>
                    {
                        var obj = Instantiate(prefab);
                        var pooleable = obj.GetComponent<IPooleable>();
                        return pooleable;
                    },
                    obj => obj.OnSpawn(), obj => obj.OnDespawn(), obj => Destroy(((MonoBehaviour)obj).gameObject), /*Cambiar esto*/ true, poolConfig._initialSize, poolConfig._maxSize );
                /*
                for (int i = 0; i < poolConfig._initialSize; i++)
                {
                    var obj = pool.Get();
                    pool.Release(obj);
                }
                */
                _pools.Add(poolConfig._id, pool);
            }
        }
        public IPooleable Get(EnemyType enemyType, Vector3 position)
        {
            _pools.TryGetValue(enemyType, out var pool);
            var entity = pool.Get() as AIBrain;
            entity.transform.position = position;
            return entity;
        }
        public void Release(EnemyType enemyType, IPooleable obj)
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
