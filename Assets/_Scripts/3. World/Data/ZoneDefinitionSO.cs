using System;
using UnityEngine;

namespace World
{
    public enum ZoneTag
    {
        First,
        Second,
        Third
    }

    [Serializable]
    public struct RoomPrefabPool
    {
        public RoomType Type;
        public GameObject[] Prefabs;
    }

    [Serializable]
    public struct RoomEncounterPool
    {
        public RoomType Type;
        public RoomEncounterSO[] Encounters;
    }

    [CreateAssetMenu(menuName = "ScriptableObjects/World/Zone Definition", fileName = "ZoneDefinition_")]
    public class ZoneDefinitionSO : ScriptableObject
    {
        [Header("Identity")] [SerializeField] private ZoneTag _zoneTag;

        [Header("Structure")] [SerializeField, Min(1)]
        private int _floorAmount = 3;
        [SerializeField, Min(4)] private int _totalRoomsPerFloor = 7;
        [SerializeField] private int _combatRoomMin = 3;
        [SerializeField] private int _combatRoomMax = 4;

        [Header("Non-Combat Pool Weights (Shop / Resting / Artifact")]
        [Tooltip("Weight every pool room type starts at.")]
        [SerializeField] private int _startingRoomWeight = 100;
        [Tooltip("Weight a room type drops to right after it's picked.")]
        [SerializeField] private int _weightPenalty = 20;
        [Tooltip("Weight every other room type gains when it's NOT picked.")]
        [SerializeField] private int _weightBonus = 40;

        [Header("Room Prefab Pools")] [SerializeField]
        private RoomPrefabPool[] _roomPrefabPools;

        [Header("Room Encounter Pools")]
        [Tooltip("Combat/Boss rooms pick a random RoomEncounterSO from here at floor-gen time. Other room types don't need an entry.")]
        [SerializeField] private RoomEncounterPool[] _roomEncounterPools;
        
        public ZoneTag ZoneTag => _zoneTag;
        public int FloorAmount => _floorAmount;
        public int TotalRoomsPerFloor => _totalRoomsPerFloor;
        public int CombatRoomMin => _combatRoomMin;
        public int CombatRoomMax => _combatRoomMax;
        public int StartingRoomWeight => _startingRoomWeight;
        public int WeightPenalty => _weightPenalty;
        public int WeightBonus => _weightBonus;
        public Material OpenDoorMaterial { get; }
        public Material WallMaterial { get; }

        public GameObject GetRandomPrefab(RoomType type, System.Random rng)
        {
            foreach (var pool in _roomPrefabPools)
            {
                if (pool.Type != type || pool.Prefabs == null || pool.Prefabs.Length == 0)
                    continue;
                
                return pool.Prefabs[rng.Next(pool.Prefabs.Length)];
            }
            
            Debug.LogError($"ZoneDefinitionSO '{name}' has no prefabs registered for {type}.");
            return null;
        }

        public RoomEncounterSO GetRandomEncounter(RoomType type, System.Random rng)
        {
            foreach (var pool in _roomEncounterPools)
            {
                if (pool.Type != type || pool.Encounters == null || pool.Encounters.Length == 0)
                    continue;

                return pool.Encounters[rng.Next(pool.Encounters.Length)];
            }

            // Unlike GetRandomPrefab, no error here — most RoomTypes legitimately
            // have no encounter (Start/Resting/Artifact/Shop/Portal). The caller
            // decides whether a miss for Combat/Boss specifically is worth a warning.
            return null;
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            var (min, max) = FloorLayoutGenerator.ClampCombatRange(_totalRoomsPerFloor, _combatRoomMin, _combatRoomMax);
            _combatRoomMin = min;
            _combatRoomMax = max;
        }
        #endif
    }
}