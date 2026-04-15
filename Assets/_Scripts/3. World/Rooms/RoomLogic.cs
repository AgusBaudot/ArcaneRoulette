using System;
using UnityEngine;

namespace World
{
    public class RoomLogic : MonoBehaviour
    {
        [SerializeField] private RoomSpawnPoint[] _spawnPoints;
        [SerializeField] private RoomDoor _doorCollider;
        [SerializeField] private GameObject _door;

        private void Start()
        {
            _doorCollider.OnPlayerEnter += OnPlayerEnter;
        }

        private void Update()
        {
            if (transform.childCount == 0)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        private void OnPlayerEnter()
        {
            foreach (var spawnPoint in _spawnPoints)
            {
                Instantiate(spawnPoint.GetEnemy(), spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                Destroy(spawnPoint.gameObject);
            }

            Destroy(_doorCollider.gameObject);
            _door.SetActive(true);
        }
    }
}