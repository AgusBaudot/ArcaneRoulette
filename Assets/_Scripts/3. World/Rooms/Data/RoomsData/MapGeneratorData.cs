using UnityEngine;

namespace World
{
    [CreateAssetMenu(fileName = "Generator", menuName = "World/Maps/MapGenerator")]
    public class MapGeneratorData : ScriptableObject
    {
        [Header("Dungeon Size")]
        [Tooltip("Minimum of regular Rooms in the grid + boss.")]
        [SerializeField, Range(3, 15)]
        public int MinRooms;
        [Tooltip("Maximum number of rooms in the grid + boss.\nMaxRoom MUST NEVER BE LESS than minRoom.")]
        [SerializeField, Range(3, 15)]
        public int MaxRooms;

        [Header("Grid Density")]
        [Tooltip("A type of room guaranteed to be surrounded by others, its not a dead end or an middle room.")]
        [SerializeField, Range(1, 6)]
        public int SurroundedRooms;
       
        [Header("Fixed Room Counts")]
        [Tooltip("How many of the minumun rooms are guaranteed to be Regular type.")]
        [Min(1)] public int MinCombatRooms;
        [Tooltip("How many of the total rooms are guaranteed to be Regular type.")]
        [Min(1)] public int MaxCombatRooms;

        [Header("Guaranteed Special Rooms")]
        [Tooltip("If true, at least 1 Resting room is guaranteed to spawn.")]
        public bool GuaranteeRestingRoom;
        [Tooltip("If true, at least 1 Shop room is guaranteed to spawn.")]
        public bool GuaranteeShopRoom;
        [Tooltip("If true, at least 1 Item room is guaranteed to spawn.")]
        public bool GuaranteeArtifactRoom;

        [Header("BossRoom")]
        [Tooltip("If true, this generation will have a Boss Room, otherwise a portal room.")]
        public bool GenerateBossRoom;

        private void OnValidate()
        {
            if (MaxRooms < MinRooms) MaxRooms = MinRooms;

            if (MaxCombatRooms < MinCombatRooms) MaxCombatRooms = MinCombatRooms;
            
            // 1-Boss/Portal + 1-Lobby + Min Combat + guaranteedSpecialRooms
            int guaranteedSpecialCount = 0;
            if (GuaranteeRestingRoom) guaranteedSpecialCount++;
            if (GuaranteeShopRoom) guaranteedSpecialCount++;
            if (GuaranteeArtifactRoom) guaranteedSpecialCount++;

            int absoluteMinRequired = 2 + MinCombatRooms + guaranteedSpecialCount;

            if (MinRooms < absoluteMinRequired)
            {
                Debug.LogWarning($"<color=yellow>MapGeneratorData [{name}]:</color> 'Min Rooms'It was increased to {absoluteMinRequired} because the current configuration (Combat, Surrounded, non-Combat) requires that minimum space to avoid breaking the algorithm.");
                MinRooms = absoluteMinRequired;
                if (MaxRooms < MinRooms) MaxRooms = MinRooms;
            }

            // MaxCombat cannot use more space than what is available.
            int maxAvailableForCombat = MaxRooms - 2 - guaranteedSpecialCount;
            if (maxAvailableForCombat < 0) maxAvailableForCombat = 0;

            if (MaxCombatRooms > maxAvailableForCombat)
            {
                MaxCombatRooms = maxAvailableForCombat;
                if (MinCombatRooms > MaxCombatRooms) MinCombatRooms = MaxCombatRooms;
            }
        }
    }

}
