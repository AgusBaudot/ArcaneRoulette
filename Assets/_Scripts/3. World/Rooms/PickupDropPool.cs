using System;
using UnityEngine;
using Foundation;

namespace World
{
    [CreateAssetMenu(menuName = "ArcaneRoulette/Drop Pools/PickupDropPool")]
    public class PickupDropPool : ScriptableObject
    {
        [SerializeField] private GameObject[] _pickupPrefabs;
        [SerializeField] private RuneDefinitionSO[] _runes;

        public GameObject GetRandomPickupPrefab()
        {
            if (_pickupPrefabs == null || _pickupPrefabs.Length == 0)
                return null;

            return _pickupPrefabs[UnityEngine.Random.Range(0, _pickupPrefabs.Length)];
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> distinct runes chosen at random
        /// via Fisher-Yates on a shallow copy. Never returns null entries.
        /// If the pool has fewer runes than requested, returns all of them.
        /// </summary>
        public RuneDefinitionSO[] GetRandomRunes(int count)
        {
            if (_runes == null || _runes.Length == 0)
                return Array.Empty<RuneDefinitionSO>();

            count = Mathf.Min(count, _runes.Length);

            var pool = new RuneDefinitionSO[_runes.Length];
            Array.Copy(_runes, pool, _runes.Length);

            for (int i = pool.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new RuneDefinitionSO[count];
            Array.Copy(pool, result, count);
            return result;
        }
    }
}