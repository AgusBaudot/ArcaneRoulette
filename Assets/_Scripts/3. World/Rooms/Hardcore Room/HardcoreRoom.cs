using System;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class HardcoreRoom : MonoBehaviour
    {
        [Header("Room references")]
        [SerializeField] private HardcoreDoor _door;
        [SerializeField] private HardcoreSpawner[] _spawners;
        
        [Header("Room Info details")]
        [SerializeField] private EnemyEntry[] _entries;
        [SerializeField] private int _waveAmount;
        
        private int _currentWave;
        private int _activeEnemies;

        private void OnEnable()
        {
            _door.OnPlayerEnter += HandlePlayerEnter;
        }

        private void OnDisable()
        {
            _door.OnPlayerEnter -= HandlePlayerEnter;
        }

        private void HandlePlayerEnter()
        {
            StartWave(_currentWave);
        }

        private void StartWave(int waveIndex)
        {
            _activeEnemies = 0;

            foreach (var spawner in _spawners)
            {
                List<BaseEnemy> spawnedEnemies = spawner.Spawn(_entries, waveIndex);

                foreach (var enemy in spawnedEnemies)
                {
                    enemy.OnDeath += HandleEnemyDeath;
                    _activeEnemies++;
                }
            }

            if (_activeEnemies == 0)
            {
                HandleWaveCompleted();
            }
        }

        private void HandleEnemyDeath(BaseEnemy deadEnemy)
        {
            deadEnemy.OnDeath -= HandleEnemyDeath;
            
            _activeEnemies--;

            if (_activeEnemies <= 0)
            {
                HandleWaveCompleted();
            }
        }

        private void HandleWaveCompleted()
        {
            _currentWave++;

            if (_currentWave < _waveAmount)
            {
                StartWave(_currentWave);
            }
            else
            {
                Debug.Log("Room Cleared! All waves defeated.");
                // EventBus.Publish(new RoomManager.RoomClearEvent());
            }
        }

        //Method subscription to event in spawners for enemy count? How to listen to enemy death?
    }

    [Serializable]
    public struct EnemyEntry
    {
        public BaseEnemy Enemy;
        [Range(0, 1)] public float Chance;
    } 
}