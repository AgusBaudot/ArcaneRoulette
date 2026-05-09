using Foundation;
using UnityEngine;

namespace World 
{
    public enum RoomState
    {
        Idle,
        Active,
        Cleared,
        Reward,
        Unlocked
    }
    public class RoomManager : MonoBehaviour , IRoom
    {
        public int Index => throw new System.NotImplementedException();
        public int Value => throw new System.NotImplementedException();
        public RoomType RoomType => throw new System.NotImplementedException();
        public RoomState State => throw new System.NotImplementedException();

        [Header("Room spawn settings")]
        [SerializeField]
        private Transform[] _spawnMelee;

        [SerializeField] private Transform[] _spawnRanged;
        [SerializeField] private Transform[] _spawnHealer;

        

        [SerializeField] private RoomState _state;
        [SerializeField] private GameObject _enemyPrefabMelee;
        [SerializeField] public int _enemyMeleeCount;
        [SerializeField] private GameObject _enemyPrefabRange;
        [SerializeField] private GameObject _enemyPrefabHealer;
        [SerializeField] public int _enemyRangeCount;
        [SerializeField] int enemiesAlive = 0;

        

        


        private void Start()
        {
            _state = RoomState.Idle;
        }

        private void Activate(Collider playerCollider)
        {
            if (_state == RoomState.Idle)
            {
                _state = RoomState.Active;
                var player = playerCollider.transform;
                foreach (var spawnPoint in _spawnMelee)
                {
                    var enemy = Instantiate(_enemyPrefabMelee, spawnPoint.transform.position, spawnPoint.transform.rotation,
                        transform);
                    enemiesAlive++;
                    enemy.GetComponent<BaseEnemy>()?.Init(player);
                    enemy.GetComponent<EnemyHealth>().OnDeath += OnEnemyDeath;
                    // enemy.GetComponent<DummyEnemy>().OnDeath += OnEnemyDeath;
                }

                foreach (var spawnPoint in _spawnRanged)
                {
                    var enemy = Instantiate(_enemyPrefabRange, spawnPoint.transform.position, spawnPoint.transform.rotation,
                        transform);
                    enemiesAlive++;
                    enemy.GetComponent<BaseEnemy>()?.Init(player);
                    enemy.GetComponent<EnemyHealth>().OnDeath += OnEnemyDeath;
                }

                foreach (var spawnPoint in _spawnHealer)
                {
                    var enemy = Instantiate(_enemyPrefabHealer, spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                    enemiesAlive++;
                    enemy.GetComponent<BaseEnemy>()?.Init(player);
                    enemy.GetComponent<EnemyHealth>().OnDeath += OnEnemyDeath;
                }
                //Destroy(spawnPoint.gameObject);
            }
        }

        private void OnEnemyDeath()
        {
            enemiesAlive--;
            if (enemiesAlive <= 0)
            {
                _state = RoomState.Cleared;
                Destroy(_door1.gameObject);
                Destroy(_door2.gameObject);
                //EventBus.Publish(new RoomClearEvent { roomId = _roomId });
            }

        }
    }
}

            //EventBus.Publish(new RoomClearEvent { roomId = _roomId });
        //public struct RoomClearEvent
        //{
        //    public int roomId;
        //}