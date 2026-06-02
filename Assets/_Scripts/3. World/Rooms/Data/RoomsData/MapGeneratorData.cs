using UnityEngine;

namespace World 
{
    [CreateAssetMenu(fileName = "Generator", menuName = "World/Maps/MapGenerator")]
    public class MapGeneratorData : ScriptableObject
    {
        [Header("Dungeon Size")]
        [Tooltip("Minimum of regular Rooms in the grid + boss.")]
        public int MinRooms;
        [Tooltip("Maximum number of rooms in the grid + boss.\nMaxRoom MUST NEVER BE LESS than minRoom.")]
        public int MaxRooms;

        [Header("Fixed Room Counts")]
        [Tooltip("How many of the minumun rooms are guaranteed to be Regular type.")]
        public int MinRegularRooms;
        [Tooltip("How many of the total rooms are guaranteed to be Regular type.")]
        public int MaxRegularRooms;

        [Header("Guaranteed Special Rooms")]
        [Tooltip("If true, at least 1 Resting room is guaranteed to spawn.")]
        public bool GuaranteeRestingRoom;
        [Tooltip("If true, at least 1 Shop room is guaranteed to spawn.")]
        public bool GuaranteeShopRoom;
        [Tooltip("If true, at least 1 Secret room is guaranteed to spawn.")]
        public bool GuaranteeEventRoom;
        [Tooltip("If true, at least 1 Item room is guaranteed to spawn.")]
        public bool GuaranteeArtifactRoom;
    }

}
