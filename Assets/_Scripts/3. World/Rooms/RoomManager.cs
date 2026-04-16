using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private RoomDoor _doorCollider;
    [SerializeField] private GameObject _door;
    [SerializeField] private RoomState _state;
    [SerializeField] private GameObject _player;
    [SerializeField] private Transform _playerSpawn;

    [SerializeField] private GameObject _enemyPrefabMelee;
    [SerializeField] public int _enemyMeleeCount;
    [SerializeField] private GameObject _enemyPrefabRange;
    [SerializeField] public int _enemyRangeCount;

    int enemiesAlive = 0;
    public event Action StateChanged;


    public RoomState GetRoomState() => _state;
    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _doorCollider.OnPlayerEnter += OnPlayerEnter;
        _state = RoomState.Idle;
    }

    private void Update()
    {
        /*
        if (transform.childCount == 0)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        */
    }



    private void OnPlayerEnter()
    {
        _state = RoomState.Active;
        _player.transform.position = _playerSpawn.position; //TeleportPlayer

        for (int i = 0; i < _enemyMeleeCount; i++ )
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

        Destroy(_doorCollider.gameObject);
        _door.SetActive(true);
        //Destroy(spawnPoint.gameObject);
    }

    private void OnEnemyDeath() 
    {
        enemiesAlive--;
        if (enemiesAlive <= 0)
            _state = RoomState.Cleared;
    }
}
