using System;
using UnityEngine;

namespace World
{
    public class HardcoreSpawner : MonoBehaviour
    {
        [SerializeField] private WaveEntry[] _waves;

        public void Spawn(EnemyEntry[] entries)
        {
            foreach (EnemyEntry entry in entries)
            {

            }
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
