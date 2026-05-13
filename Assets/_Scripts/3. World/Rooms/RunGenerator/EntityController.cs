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
        private RoomEncounterData _encounterData;
        private RoomState _state;

        [Header("Hazards")]
        [SerializeField] MonoBehaviour[] hazards; //overkill me quedo sin tiempo xd


        [SerializeField] private GameObject _enemyPrefabMelee;
        [SerializeField] public int _enemyMeleeCount;
        [SerializeField] private GameObject _enemyPrefabRange;
        [SerializeField] public int _enemyRangeCount;
        int enemiesAlive = 0;

        public event Action RoomIsClear;
        public void SaveEnemiesData(RoomEncounterData encounterData) 
        {
            _encounterData = encounterData;
        }
        public void DisableAllHazards() 
        {
            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] is IHazard hazard)
                    hazard.Disable();
            }
        }
        private void SpawnEnemies(RoomEncounterData data)
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
