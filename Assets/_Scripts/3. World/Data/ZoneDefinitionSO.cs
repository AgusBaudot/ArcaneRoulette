using System;
using UnityEngine;

namespace World
{
    public enum ZoneTag { First, Second, Third, Fourth }

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

        [Header("Structure")] 
        [SerializeField, Min(1)] private int _floorAmount = 2;
        [Tooltip("If true, the final floor of this zone generates a Boss room. If false, it generates a standard Portal/Exit room.")]
        [SerializeField] private bool _hasBossEncounter = false;
        [SerializeField, Min(4)] private int _totalRoomsPerFloor = 7;
        [SerializeField] private int _combatRoomMin = 3;
        [SerializeField] private int _combatRoomMax = 4;

        [Header("Encounter Selection")]
        [Tooltip("If true, encounters are selected by their list index sequentially per floor. If false, they are picked randomly.")]
        [SerializeField] private bool _sequentialEncounters;

        [Header("Non-Combat Pool Weights (Shop / Resting / Artifact)")]
        [SerializeField] private int _startingRoomWeight = 100;
        [SerializeField] private int _weightPenalty = 20;
        [SerializeField] private int _weightBonus = 40;

        [Header("Room Prefab Pools")] 
        [SerializeField] private RoomPrefabPool[] _roomPrefabPools;

        [Header("Room Encounter Pools")]
        [SerializeField] private RoomEncounterPool[] _roomEncounterPools;
        
        public ZoneTag ZoneTag => _zoneTag;
        public int FloorAmount => _floorAmount;
        public bool HasBossEncounter => _hasBossEncounter;
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

        public RoomEncounterSO GetEncounter(RoomType type, System.Random rng, int indexWithinType)
        {
            foreach (var pool in _roomEncounterPools)
            {
                if (pool.Type != type || pool.Encounters == null || pool.Encounters.Length == 0)
                    continue;

                if (_sequentialEncounters)
                    return pool.Encounters[indexWithinType % pool.Encounters.Length];
                else
                    return pool.Encounters[rng.Next(pool.Encounters.Length)];
            }

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