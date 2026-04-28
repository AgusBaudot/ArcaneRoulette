using UnityEngine;

namespace World
{
    [CreateAssetMenu(menuName = "ArcaneRoulette/Drop Pools/PickupDropPool")]
    public class PickupDropPool : ScriptableObject
    {
        [SerializeField] private GameObject[] _pickupPrefabs;

        public GameObject GetRandomPickupPrefab()
        {
            if (_pickupPrefabs == null || _pickupPrefabs.Length == 0)
                return null;

            return _pickupPrefabs[Random.Range(0, _pickupPrefabs.Length)];
        }
    }
}
