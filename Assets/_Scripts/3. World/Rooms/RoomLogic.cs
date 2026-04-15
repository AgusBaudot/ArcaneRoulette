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
                Debug.LogError($"{nameof(RoomLogic)}: All enemies defeated! Rune dropping...");
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