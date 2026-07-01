using UnityEngine;

namespace World 
{
    [CreateAssetMenu(fileName = "Spawner", menuName = "World/Maps/MapSpawner")]
    public class MapSpawnerData : ScriptableObject
    {
        [Tooltip("Regular = Combat room with normal enemies.")]
        public RoomManager[] CombatRoomPrefab;
        [Tooltip("Resting = Passive room where you can heal.")]
        public RoomManager RestingRoomPrefab;
        [Tooltip("Shop = Passive room where you can buy something.")]
        public RoomManager ShopRoomPrefab;
        [Tooltip("Boss = Combat room with floor boss.")]
        public RoomManager BossRoomPrefab;
        [Tooltip("Lobby = Entry room of the floor.")]
        public RoomManager LobbyRoomPrefab;
        [Tooltip("Secret = just a normal room surrounded by many rooms.")]
        public RoomManager PortalRoomPrefab;
        [Tooltip("Secret = just a normal room surrounded by many rooms.")]
        public RoomManager ArtifactRoomPrefab;
        [Tooltip("Select Material and de RoomType for that room.")]
        public DoorScriptable[] doorsMaterials;
        [Tooltip("Offset means the space between rooms. \n (recomended > 100)")]
        public Vector2 roomOffset;
    }
}

