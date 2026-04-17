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

        private void OnPlayerEnter(Collider playerCol)
        {
            foreach (var spawnPoint in _spawnPoints)
            {
                var go = Instantiate(spawnPoint.GetEnemy(), spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                
                if (go.TryGetComponent<BasicMeleeEnemy>(out var melee))
                    melee.Init(playerCol.transform);
                
                else if (go.TryGetComponent<BasicRangedEnemy>(out var ranged))
                    ranged.Init(playerCol.transform);
                
                else if (go.TryGetComponent<BasicBruteEnemy>(out var brute))
                    ranged.Init(playerCol.transform);
                
                Destroy(spawnPoint.gameObject);
            }

            Destroy(_doorCollider.gameObject);
            _door.SetActive(true);
        }
    }
}