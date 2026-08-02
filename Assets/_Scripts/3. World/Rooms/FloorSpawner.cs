using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace World
{
    public sealed class FloorSpawner : MonoBehaviour
    {
        [Header("Zones — index 0 = First, 1 = Second, 2 = Third")] [SerializeField]
        private ZoneDefinitionSO[] _zones;

        [Header("Layout")] [SerializeField] private float _roomSpacing = 30f;

        private readonly Dictionary<int, RoomManager> _roomsByIndex = new Dictionary<int, RoomManager>();
        private IReadOnlyList<RoomLayoutNode> _currentLayout;

        private void OnEnable() => EventBus.Subscribe<RoomTransitionExecuteEvent>(HandleRoomTransitionExecute);
        private void OnDisable() => EventBus.Unsubscribe<RoomTransitionExecuteEvent>(HandleRoomTransitionExecute);
        private void Start() => SpawnCurrentFloor();

        public void SpawnCurrentFloor()
        {
            if (GameStateManager.RunState == null)
            {
                Debug.LogError("FloorSpawner: no active RunState — GameLevel loaded without a run in progress.");
                return;
            }

            int currentFloor = GameStateManager.RunState.CurrentFloor;
            int zoneIndex = (currentFloor - 1) / 3;
            bool isBossFloor = currentFloor % 3 == 0;

            if (zoneIndex < 0 || zoneIndex >= _zones.Length || _zones[zoneIndex] == null)
            {
                Debug.LogError(
                    $"FloorSpawner: no ZoneDefinitionSO for zone index {zoneIndex} (CurrentFloor={currentFloor}).");
                return;
            }

            ZoneDefinitionSO zone = _zones[zoneIndex];
            var expectedTag = (ZoneTag)zoneIndex;
            if (zone.ZoneTag != expectedTag)
                Debug.LogWarning(
                    $"FloorSpawner: _zones[{zoneIndex}] is tagged {zone.ZoneTag}, expected {expectedTag}.");

            var rng = new System.Random();
            IReadOnlyList<RoomLayoutNode> layout = FloorLayoutGenerator.Generate(zone, isBossFloor, rng);
            if (layout == null) return; // already logged why

            _currentLayout = layout;
            GameStateManager.RunState.InitializeFloorMap(BuildRoomMapData(layout));
            SpawnRooms(zone, layout, rng);

            if (!_roomsByIndex.TryGetValue(0, out var startRoom))
                return;

            startRoom.EnableRoom();

            EventBus.Publish(new PlayerTeleportRequestEvent(startRoom.gameObject.transform.position));
        }

        private void SpawnRooms(ZoneDefinitionSO zone, IReadOnlyList<RoomLayoutNode> layout, System.Random rng)
        {
            _roomsByIndex.Clear();

            foreach (var node in layout)
            {
                GameObject prefab = zone.GetRandomPrefab(node.Type, rng);
                if (prefab == null) continue;

                Vector3 worldPos = new Vector3(node.GridPosition.x * _roomSpacing, 0f,
                    node.GridPosition.y * _roomSpacing);
                GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);

                var room = instance.GetComponent<RoomManager>();
                if (room == null)
                {
                    Debug.LogError($"Room prefab '{prefab.name}' for {node.Type} is missing a RoomManager component.");
                    continue;
                }

                room.Init(new RoomInfo { index = node.Index, roomType = node.Type });
                room.InitDoors(BuildDoorsInfo(zone, node, layout));

                if (node.Type == RoomType.Combat || node.Type == RoomType.Boss)
                {
                    RoomEncounterSO encounter = zone.GetRandomEncounter(node.Type, rng);
                    if (encounter != null)
                        room.InitEntity(encounter.ToRoomEncounterData());
                    else
                        Debug.LogWarning(
                            $"FloorSpawner: no RoomEncounterSO registered for {node.Type} at room {node.Index} — it will clear on entry with no enemies.");
                }

                _roomsByIndex[node.Index] = room;
            }
        }

        private void HandleRoomTransitionExecute(RoomTransitionExecuteEvent evt)
        {
            if (_currentLayout == null) return;
            if (!_roomsByIndex.TryGetValue(evt.SourceIndex, out var sourceRoom)) return;

            RoomLayoutNode sourceNode = _currentLayout[evt.SourceIndex];
            Vector2Int delta = DirectionToDelta(evt.Direction);

            RoomLayoutNode targetNode = null;
            foreach (int neighborIndex in sourceNode.NeighborIndices)
            {
                if (_currentLayout[neighborIndex].GridPosition - sourceNode.GridPosition == delta)
                {
                    targetNode = _currentLayout[neighborIndex];
                    break;
                }
            }

            if (targetNode == null || !_roomsByIndex.TryGetValue(targetNode.Index, out var targetRoom))
            {
                Debug.LogError($"FloorSpawner: room {evt.SourceIndex} has no neighbor to the {evt.Direction}.");
                return;
            }

            Vector3 spawnPos = targetRoom.GetRoomConnections.GetPlayerSpawn(evt.Direction);

            sourceRoom.DisableRoom();
            targetRoom.EnableRoom();
            EventBus.Publish(new PlayerTeleportRequestEvent(spawnPos));
        }

        private static Vector2Int DirectionToDelta(EdgeDirection direction)
        {
            switch (direction)
            {
                case EdgeDirection.Up: return Vector2Int.up;
                case EdgeDirection.Down: return Vector2Int.down;
                case EdgeDirection.Left: return Vector2Int.left;
                case EdgeDirection.Right: return Vector2Int.right;
                default: return Vector2Int.zero;
            }
        }

        private static AllDoorsInfo BuildDoorsInfo(ZoneDefinitionSO zone, RoomLayoutNode node,
            IReadOnlyList<RoomLayoutNode> allNodes)
        {
            var open = new Dictionary<Vector2Int, bool>
            {
                { Vector2Int.up, false }, { Vector2Int.down, false },
                { Vector2Int.left, false }, { Vector2Int.right, false }
            };
            foreach (int neighborIndex in node.NeighborIndices)
            {
                Vector2Int delta = allNodes[neighborIndex].GridPosition - node.GridPosition;
                if (open.ContainsKey(delta)) open[delta] = true;
            }

            return new AllDoorsInfo
            {
                Up = BuildDoorInfo(open[Vector2Int.up], zone),
                Down = BuildDoorInfo(open[Vector2Int.down], zone),
                Left = BuildDoorInfo(open[Vector2Int.left], zone),
                Right = BuildDoorInfo(open[Vector2Int.right], zone)
            };
        }

        private static DoorInfo BuildDoorInfo(bool isOpen, ZoneDefinitionSO zone) => new DoorInfo
        {
            Material = isOpen ? zone.OpenDoorMaterial : zone.WallMaterial,
            UnlockOnClear = isOpen
        };

        private static Dictionary<int, VolatileRunState.RoomMapData> BuildRoomMapData(
            IReadOnlyList<RoomLayoutNode> layout)
        {
            var map = new Dictionary<int, VolatileRunState.RoomMapData>();
            foreach (var node in layout)
            {
                map[node.Index] = new VolatileRunState.RoomMapData
                {
                    Index = node.Index,
                    X = node.GridPosition.x,
                    Y = node.GridPosition.y,
                    Type = node.Type,
                    IsCleared = false,
                    IsDiscovered = false,
                    NeighborIndices = node.NeighborIndices.ToArray()
                };
            }

            return map;
        }
    }
}