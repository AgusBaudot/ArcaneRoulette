using UnityEngine;

namespace World
{
    public class RoomSpawnPoint : MonoBehaviour
    {
        [SerializeField] private GameObject _enemyPrefab;

        public GameObject GetEnemy() => _enemyPrefab;
    }
}