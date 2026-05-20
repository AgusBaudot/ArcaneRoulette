using System;
using UnityEngine;

namespace World
{
    public class HardcoreRoom : MonoBehaviour
    {
        [Header("Room references")]
        [SerializeField] private HardcoreDoor _door;
        [Header("Room Info details")]
        [SerializeField] private EnemyEntry[] _entries;
        [SerializeField] private int _waveAmount;

        private void OnEnable()
        {
            _door.OnPlayerEnter += HandlePlayerEnter;
        }

        private void OnDisable()
        {
            _door.OnPlayerEnter -= HandlePlayerEnter;
        }

        private void HandlePlayerEnter()
        {
            Debug.LogError("Player enter");
        }
    }

    [Serializable]
    public struct EnemyEntry
    {
        public BaseEnemy _enemy;
        [Range(0, 1)] public float _chance;
    } 
}