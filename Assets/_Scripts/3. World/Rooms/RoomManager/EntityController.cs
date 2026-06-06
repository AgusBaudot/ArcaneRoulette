using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Random = UnityEngine.Random;

namespace World
{
    public class EntityController : MonoBehaviour
    {
        [Header("Room spawn settings")]
        [SerializeField] private BoxCollider[] _enemySpawns;
        [SerializeField] private int _spawnAtSameTime;
        [SerializeField] private int _enemiesAlive = 0;
        [SerializeField] private int _currentWave = 0;
        private List<IPoolable> _spawnedEnemies = new List<IPoolable>();
        private RoomEncounterData _encounterData;

        [Header("Hazards")]
        [SerializeField] MonoBehaviour[] hazards; //overkill me quedo sin tiempo xd

        public event Action RoomIsClear;
        public void SaveEnemiesData(RoomEncounterData encounterData)
        {
            _encounterData = encounterData;
            _currentWave = 0;
        }
        private Vector3 GetRandomSpawnPosition(int spawn)
        {
            Bounds bounds = _enemySpawns[spawn].bounds;
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.min.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }
        public void SpawnEnemies()
        {
            if (_encounterData.Waves == null || _encounterData.Waves.Length == 0)
            {
                RoomIsClear?.Invoke();
                return;
            }
            SpawnWave(_currentWave);
        }
        private void SpawnWave(int waveIndex)
        {
            EnemySpawnData wave = _encounterData.Waves[waveIndex];
            _enemiesAlive = 0;

            for (int i = 0; i < wave.EnemyType.Length; i++)
            {
                EnemyType type = wave.EnemyType[i];
                int amount = wave.Amounts[i];

                for (int j = 0; j < amount; j++)
                {
                    if (_enemySpawns.Length == 0)
                    {
                        continue;
                    }

                    int spawnIndex = _enemiesAlive % _enemySpawns.Length;
                    Vector3 spawn = GetRandomSpawnPosition(spawnIndex);
                    IPoolable enemy = PoolEnemy.Instance.Get(type, spawn);

                    // el enemigo avisa cuando muere
                    if (enemy is EnemyController ec)
                    {
                        ec.Type = type;
                        ec.OnDeathEvent -= OnEnemyDeath;
                        ec.OnDeathEvent += OnEnemyDeath;
                        StartCoroutine(VerifySubscription(ec));
                    }

                    _spawnedEnemies.Add(enemy);
                    _enemiesAlive++;
                }
            }
        }
        private void OnEnemyDeath(EnemyController enemy)
        {
            PoolEnemy.Instance.Release(enemy.Type, enemy);
            _enemiesAlive--;

            if (_enemiesAlive <= 0)
            {
                _currentWave++;
                if (_currentWave < _encounterData.Waves.Length)
                    SpawnWave(_currentWave);
                else
                    RoomIsClear?.Invoke();
            }
        }
        public void DisableAllHazards()
        {
            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] is IHazard hazard)
                    hazard.Disable();
            }
        }
        private IEnumerator VerifySubscription(EnemyController ec)
        {
            int attempts = 0;
            int maxAttempts = 10;

            while (attempts < maxAttempts)
            {
                yield return null; // espera un frame

                if (ec == null || !ec.gameObject.activeSelf) yield break;

                if (!ec.HasDeathListeners())
                {
                    Debug.LogWarning($"[VerifySubscription] {ec.name} sin listeners, resuscribiendo. Intento {attempts + 1}");
                    ec.OnDeathEvent -= OnEnemyDeath;
                    ec.OnDeathEvent += OnEnemyDeath;
                }
                else
                {
                    //Debug.Log($"[VerifySubscription] {ec.name} suscripto correctamente");
                    yield break; // todo bien, salir
                }

                attempts++;
            }

            Debug.LogError($"[VerifySubscription] {ec.name} no pudo suscribirse despu�s de {maxAttempts} intentos");
        }
    }

}
