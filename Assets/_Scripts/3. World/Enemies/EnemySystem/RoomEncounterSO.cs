using System;
using UnityEngine;

namespace World
{
    /// <summary>
    /// Designer-facing authoring asset for RoomEncounterData — the piece
    /// FloorSpawner's comment calls "no encounter authoring pipeline yet".
    /// Doesn't touch EntityController; converts into the RoomEncounterData/
    /// EnemySpawnData it already consumes via SaveEnemiesData.
    ///
    /// WaveDefinition mirrors EnemySpawnData's two fields (EnemyType[] +
    /// int[] Amounts, confirmed from EntityController.SpawnWave's usage)
    /// instead of serializing EnemySpawnData directly — safer without
    /// knowing for certain whether EnemySpawnData itself is [Serializable].
    /// </summary>
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

        [SerializeField] private WaveDefinition[] _waves;

        /// <summary>
        /// Builds the value EntityController.SaveEnemiesData expects. Where
        /// this gets called from — FloorSpawner, most likely, right before
        /// RoomManager.InitEntity — is still open; see chat.
        /// </summary>
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
            return new RoomEncounterData { Waves = waves };
        }
    }
}