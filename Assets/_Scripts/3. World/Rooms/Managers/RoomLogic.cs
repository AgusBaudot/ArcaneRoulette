using System;
using UnityEngine;

namespace World
{
    public class RoomLogic : MonoBehaviour
    {
        [Header("Enemy Spawn Points")]
        [SerializeField] private Transform[] _meleeSpawnPoints;
        [SerializeField] private Transform[] _rangedSpawnPoints;
        [SerializeField] private Transform[] _healerSpawnPoints;
        [Header("Enemies to spawn")]
        [SerializeField] private BaseEnemy _melee;
        [SerializeField] private BaseEnemy _ranged;
        [SerializeField] private BaseEnemy _healer;
        [Header("Room Settings")]
        [SerializeField] private RoomDoor _doorCollider;
        [SerializeField] private GameObject _door;

        private void Start()
        {
            //_doorCollider.OnPlayerEnter += OnPlayerEnter;
        }

        private void Update()
        {
            if (transform.childCount == 0)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        private void OnPlayerEnter(Collider playerCol)
        {
            foreach (var spawnPoint in _meleeSpawnPoints)
            {
                var enemy = Instantiate(_melee, spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                enemy.Init(playerCol.transform);
                Destroy(spawnPoint.gameObject);
            }

            foreach (var spawnPoint in _rangedSpawnPoints)
            {
                var enemy = Instantiate(_ranged, spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                enemy.Init(playerCol.transform);
                Destroy(spawnPoint.gameObject);
            }

            foreach (var spawnPoint in _healerSpawnPoints)
            {
                var enemy = Instantiate(_healer, spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                enemy.Init(playerCol.transform);
                Destroy(spawnPoint.gameObject);
            }
            
            Destroy(_doorCollider.gameObject);
            _door.SetActive(true);
        }
    }
}