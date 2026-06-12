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
        [SerializeField] private float _spawnDelay;
        [SerializeField] private GameObject _dangerImage;
        [SerializeField] private GameObject _effect;
        [SerializeField] private float _warningDuration;


        [Header("Hazards")]
        [SerializeField] MonoBehaviour[] hazards; //overkill

        [Header("Read only info")]
        [SerializeField] private int _enemiesAlive;
        [SerializeField] private int _currentWave;

        private List<IPoolable> _spawnedEnemies = new List<IPoolable>();
        private RoomEncounterData _encounterData;
        private List<EnemyType> _spawnList;


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

        // ---- Entry function ----
        public void PlayEntityController()
        {
            if (_encounterData.Waves == null || _encounterData.Waves.Length == 0 || _enemySpawns.Length == 0)
            {
                RoomIsClear?.Invoke();
                return;
            }
            SpawnWave(_currentWave);
        }

        // ---- Core Wave System ----
        private void SpawnWave(int waveIndex)
        {
            EnemySpawnData wave = _encounterData.Waves[waveIndex];
            _enemiesAlive = 0;
            // ---- Create a list with all enemy Types ----
            _spawnList = new List<EnemyType>();
            for (int i = 0; i < wave.EnemyType.Length; i++)
            {
                for (int j = 0; j < wave.Amounts[i]; j++)
                {
                    _spawnList.Add(wave.EnemyType[i]);
                    _enemiesAlive++;
                }
            }
            // ---- shuffle the list ----
            for (int i = _spawnList.Count - 1; i > 0; i--)
            {
                int rand = Random.Range(0, i + 1);
                EnemyType e = _spawnList[i];
                _spawnList[i] = _spawnList[rand];
                _spawnList[rand] = e;
            }

            StartCoroutine(SpawnEnemies(_spawnList));
        }
        private IEnumerator SpawnEnemies(List<EnemyType> enemiesToSpawn)
        {
            int spawnedSoFar = 0;

            while (spawnedSoFar < enemiesToSpawn.Count)
            {
                int batchSize = Mathf.Min(_spawnAtSameTime, enemiesToSpawn.Count - spawnedSoFar);
                List<Vector3> batchPositions = new List<Vector3>();
                List<GameObject> batchIndicators = new List<GameObject>();

                for (int i = 0; i < batchSize; i++)
                {
                    int spawnIndex = (spawnedSoFar + i) % _enemySpawns.Length;
                    Vector3 spawn = GetRandomSpawnPosition(spawnIndex);
                    batchPositions.Add(spawn);
                    GameObject indicator = Instantiate(_dangerImage, spawn, Quaternion.Euler(new Vector3(30, 0, 0))); // the exact rotation as the camera
                    batchIndicators.Add(indicator);
                }
                yield return new WaitForSeconds(_warningDuration);

                for (int i = 0; i < batchSize; i++)
                {
                    GameObject effect = Instantiate(_effect, batchPositions[i], Quaternion.identity);
                    Destroy(effect, 3f);
                    EnemyType type = enemiesToSpawn[spawnedSoFar + i];
                    IPoolable enemy = PoolEnemy.Instance.Get(type, batchPositions[i]);

                    if (enemy is EnemyController ec)
                    {
                        ec.Type = type;
                        ec.OnDeathEvent -= OnEnemyDeath;
                        ec.OnDeathEvent += OnEnemyDeath;
                        StartCoroutine(VerifySubscription(ec));
                    }

                    _spawnedEnemies.Add(enemy);

                    Destroy(batchIndicators[i].gameObject);
                }

                spawnedSoFar += batchSize;

                if (spawnedSoFar < enemiesToSpawn.Count)
                    yield return new WaitForSeconds(_spawnDelay);
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
        private IEnumerator VerifySubscription(EnemyController ec)
        {
            int attempts = 0;
            int maxAttempts = 10;

            while (attempts < maxAttempts)
            {
                yield return null;

                if (ec == null || !ec.gameObject.activeSelf) yield break;

                if (!ec.HasDeathListeners())
                {
                    ec.OnDeathEvent -= OnEnemyDeath;
                    ec.OnDeathEvent += OnEnemyDeath;
                }
                else
                {
                    yield break;
                }

                attempts++;
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
    }

}
