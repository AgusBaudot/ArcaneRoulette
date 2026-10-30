using System;
using UnityEngine;

namespace World
{
    [CreateAssetMenu(menuName = "ArcaneRoulette/World/Room Encounter", fileName = "RoomEncounter_")]
    public sealed class RoomEncounterSO : ScriptableObject
    {
        [Serializable]
        public struct EntryCount
        {
            public EnemyType Type;
            [Min(1)] public int Count;
        }

        [Serializable]
        public struct WaveDefinition
        {
            public EntryCount[] Entries;
        }

        [Header("Spawn Behavior")]
        [Tooltip("If true, replaces enemies 1-to-1 as they die. If false, waits for the entire wave to die before spawning the next.")]
        [SerializeField] private bool _dynamicRefill;

        [SerializeField] private WaveDefinition[] _waves;

        public RoomEncounterData ToRoomEncounterData()
        {
            var waves = new EnemySpawnData[_waves.Length];
            for (int w = 0; w < _waves.Length; w++)
            {
                EntryCount[] entries = _waves[w].Entries;
                var types = new EnemyType[entries.Length];
                var amounts = new int[entries.Length];
                for (int e = 0; e < entries.Length; e++)
                {
                    types[e] = entries[e].Type;
                    amounts[e] = entries[e].Count;
                }
                waves[w] = new EnemySpawnData { EnemyType = types, Amounts = amounts };
            }
            return new RoomEncounterData { 
                Waves = waves, 
                IsDynamicRefill = _dynamicRefill 
            };
        }
    }
}