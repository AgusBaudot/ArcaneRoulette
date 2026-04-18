using System;
using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;
using World;
using static UnityEngine.GraphicsBuffer;

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

    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private RoomDoor _activateDoor;
    [SerializeField] private RoomDoor _ExitDoor;
    [SerializeField] private RoomDoor _ContinueDoor;
    [SerializeField] private GameObject _door1;
    [SerializeField] private GameObject _door2;
    [SerializeField] private RoomState _state;
    [SerializeField] private Transform _playerSpawnEntry;
    [SerializeField] private Transform _playerSpawnExit;
    [SerializeField] public int _roomId;
    [SerializeField] private GameObject _enemyPrefabMelee;
    [SerializeField] public int _enemyMeleeCount;
    [SerializeField] private GameObject _enemyPrefabRange;
    [SerializeField] public int _enemyRangeCount;

    int enemiesAlive = 0;

    public RoomState GetRoomState() => _state;
    private void Start()
    {
        //_player = GameObject.FindGameObjectWithTag("Player");
        _activateDoor.OnPlayerEnter += Activate;
        _ExitDoor.OnPlayerEnter += ExitRoom;
        _ContinueDoor.OnPlayerEnter += ContinueRoom;
        _state = RoomState.Idle;
    }

    public struct RoomClearEvent
    {
        public int roomId;
    }
    private void Activate(Collider playerCollider)
    {
        
        if(_state == RoomState.Idle) 
        {
            _state = RoomState.Active;
            //_player.transform.position = _playerSpawn.position; //TeleportPlayer

            for (int i = 0; i < _enemyMeleeCount; i++)
            {
                foreach (var spawnPoint in _spawnPoints)
                {
                    var enemy = Instantiate(_enemyPrefabMelee, spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                    enemiesAlive++;
                    enemy.GetComponent<DummyEnemy>().OnDeath += OnEnemyDeath;
                }
            }
            for (int i = 0; i < _enemyRangeCount; i++)
            {
                foreach (var spawnPoint in _spawnPoints)
                {
                    Instantiate(_enemyPrefabRange, spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                    enemiesAlive++;
                }
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
        }
        EventBus.Publish(new RoomClearEvent { roomId = _roomId });
    }

    public void ExitRoom(Collider playerCollider) 
    {
       
    }
    public void ContinueRoom(Collider playerCollider) 
    {
        _state = RoomState.Unlocked;
    }
}
