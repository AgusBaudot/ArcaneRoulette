using UnityEngine;

namespace World 
{
    [CreateAssetMenu(fileName = "Generator", menuName = "World/Maps/MapGenerator")]
    public class MapGeneratorData : ScriptableObject
    {
        [Header("Dungeon Size")]
        [Tooltip("Minimum of regular Rooms in the grid + boss.")]
        public int minRooms;
        [Tooltip("Maximum number of rooms in the grid + boss.\nMaxRoom MUST NEVER BE LESS than minRoom.")]
        public int maxRooms;


        [Header("Special Room Counts")]
        [Tooltip("Amount of Boos rooms in this generation.")]
        public int targetBossRoom;
        [Tooltip("Amount of Resting rooms in this generation.")]
        public int targetRestingRoom;
        [Tooltip("Amount of Shop rooms in this generation.")]
        public int targetShopRooms;
        [Tooltip("Amount of Secret rooms in this generation.")]
        public int targetSecretRooms;
    }

}
