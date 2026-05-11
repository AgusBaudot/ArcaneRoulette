using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class EntityController : MonoBehaviour
    {
        [Header("Room spawn settings")]
        [SerializeField] private Transform[] _enemySpawns;

        private RoomState _state;
        [SerializeField] private GameObject _enemyPrefabMelee;
        [SerializeField] public int _enemyMeleeCount;
        [SerializeField] private GameObject _enemyPrefabRange;
        [SerializeField] public int _enemyRangeCount;
        int enemiesAlive = 0;

        public event Action RoomIsClear;
        private void Activate(Collider playerCollider)
        {
            if (_state == RoomState.Idle)
            {
                /*
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
                */
            }
        }

        private void OnEnemyDeath()
        {
            enemiesAlive--;
            if (enemiesAlive <= 0)
            {
                _state = RoomState.Cleared;
                RoomIsClear?.Invoke();
            }

        }
    }

}
