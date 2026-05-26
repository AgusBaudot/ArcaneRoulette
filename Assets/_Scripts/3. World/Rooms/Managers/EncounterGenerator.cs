using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class EncounterGenerator : MonoBehaviour
    {
        [Header("Wave Settings")]
        [SerializeField] private int _minWaves = 1;
        [SerializeField] private int _maxWaves = 3;
        [SerializeField] private int _enemiesPerWave = 2;

        [Header("Amount Settings")]
        [SerializeField] private int _minAmount = 1;
        [SerializeField] private int _maxAmount = 4;

        // el peso es el mismo orden que el enum
        private readonly float[] _weights = { 0.5f, 0.4f, 0.2f }; // melee, range, bruto

        public RoomEncounterData Generate(RoomType roomType, int roomsVisited)
        {
            if (roomType != RoomType.Regular && roomType != RoomType.Boss) // saber si es una room peleable no le corresponde a esto
                return default;

            int waveCount = Random.Range(_minWaves, _maxWaves);
            EnemySpawnData[] waves = new EnemySpawnData[waveCount];

            for (int i = 0; i < waveCount; i++)
                waves[i] = GenerateWave(roomsVisited);

            return new RoomEncounterData { Waves = waves };
        }

        private EnemySpawnData GenerateWave(int roomsVisited)
        {
            EnemyType[] types = new EnemyType[_enemiesPerWave]; 
            int[] amounts = new int[_enemiesPerWave];

            bool[] used = new bool[_weights.Length];

            for (int i = 0; i < _enemiesPerWave; i++)
            {
                types[i] = RouletteSelect(used);
                used[(int)types[i]] = true;
                amounts[i] = Random.Range(_minAmount + roomsVisited / 2 , _maxAmount + roomsVisited / 2);
            }

            return new EnemySpawnData { EnemyType = types, Amounts = amounts };
        }

        private EnemyType RouletteSelect(bool[] used)
        {
            float total = 0f;

            for (int i = 0; i < _weights.Length; i++)
                if (!used[i]) total += _weights[i];

            float roll = Random.value * total;
            float cumulative = 0f;

            for (int i = 0; i < _weights.Length; i++)
            {
                if (used[i]) continue;
                cumulative += _weights[i];
                if (roll <= cumulative)
                    return (EnemyType)i;
            }

            
            for (int i = 0; i < _weights.Length; i++)
                if (!used[i]) return (EnemyType)i;

            return EnemyType.melee;
        }
    }
}
