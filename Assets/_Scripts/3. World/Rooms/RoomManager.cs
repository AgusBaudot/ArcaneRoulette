using Core;
using Foundation;
using UnityEngine;
using World;

public class RoomManager : MonoBehaviour
{
    public enum RoomState
    {
        Idle,
        Active,
        Cleared,
        Reward,
        Unlocked
    }

    [Header("Room spawn settings")] [SerializeField]
    private Transform[] _spawnMelee;

    [SerializeField] private Transform[] _spawnRanged;
    [SerializeField] private Transform[] _spawnHealer;

    [Header("Room Settings")] [SerializeField]
    private RoomDoor _activateDoor;

    [SerializeField] private RoomDoor _ExitDoor;
    [SerializeField] public RoomDoor _ContinueDoor;
    [SerializeField] private GameObject _door1;
    [SerializeField] private GameObject _door2;
    [SerializeField] private RoomState _state;
    [SerializeField] private Transform _playerSpawnEntry;
    [SerializeField] private Transform _playerSpawnExit;
    [SerializeField] public int _roomId;
    [SerializeField] private GameObject _enemyPrefabMelee;
    [SerializeField] public int _enemyMeleeCount;
    [SerializeField] private GameObject _enemyPrefabRange;
    [SerializeField] private GameObject _enemyPrefabHealer;
    [SerializeField] public int _enemyRangeCount;
    [SerializeField] int enemiesAlive = 0;


    public Transform GetPlayerSpawnEntry() => _playerSpawnEntry;
    public Transform GetPlayerSpawnExit() => _playerSpawnExit;
    public RoomState GetRoomState() => _state;
    
    private void Start()
    {
        _activateDoor.OnPlayerEnter += Activate;
        _ExitDoor.OnPlayerEnter += ExitRoom;
        _ContinueDoor.OnPlayerEnter += ContinueRoom;
        _state = RoomState.Idle;
    }

    public struct RoomClearEvent
    {
        public int roomId;
    }
    private void Activate(EdgeDirection _)
    {
        var playerCollider = FindObjectOfType<PlayerController>().GetComponent<Collider>();
        
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
                var enemy = Instantiate(_enemyPrefabHealer,  spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                enemiesAlive++;
                enemy.GetComponent<BaseEnemy>()?.Init(player);
                enemy.GetComponent<EnemyHealth>().OnDeath += OnEnemyDeath;
            }

            Destroy(_activateDoor.gameObject);
            _door1.SetActive(true);
            _door2.SetActive(true);
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
            EventBus.Publish(new RoomClearEvent { roomId = _roomId });
        }
        
    }

    public void ExitRoom(EdgeDirection _)
    {
        EventBus.Publish(new RoomClearEvent { roomId = _roomId });
    }
    
    public void ContinueRoom(EdgeDirection _) 
    {
        _state = RoomState.Unlocked;
    }
}