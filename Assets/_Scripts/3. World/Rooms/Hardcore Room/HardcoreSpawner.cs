using System;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class HardcoreSpawner : MonoBehaviour
    {
        [SerializeField] private WaveEntry[] _waves;

        public List<BaseEnemy> Spawn(EnemyEntry[] entries, int waveNumber)
        {
            List<BaseEnemy> spawned = new();
            
            WaveEntry waveConfig = Array.Find(_waves, x => x.WaveNumber == waveNumber);

            if (waveConfig.Equals(default(WaveEntry)))
            {
                Debug.LogWarning("Passed wrong wave number.");
                return spawned;
            }
            
            int spawnCount = UnityEngine.Random.Range(waveConfig.MinimumEnemies, waveConfig.MaximumEnemies + 1);

            float totalWeight = 0f;
            foreach (var entry in entries)
            {
                totalWeight += entry.Chance;
            }

            for (int i = 0; i < spawnCount; i++)
            {
                BaseEnemy selectedPrefab = GetEnemyByWeight(entries, totalWeight);

                if (selectedPrefab != null)
                {
                    BaseEnemy instance = Instantiate(selectedPrefab, transform.position, Quaternion.identity);
                    spawned.Add(instance);
                }
            }
            
            return spawned;
        }

        private BaseEnemy GetEnemyByWeight(EnemyEntry[] entries, float totalWeight)
        {
            float randomVal = UnityEngine.Random.Range(0f, totalWeight);

            foreach (var entry in entries)
            {
                randomVal -= entry.Chance;
                if (randomVal <= 0f)
                {
                    return entry.Enemy;
                }
            }

            return entries[^1].Enemy;
        }
    }

    [Serializable]
    public struct WaveEntry
    {
        public int WaveNumber;
        public int MinimumEnemies;
        public int MaximumEnemies;
    }
}
