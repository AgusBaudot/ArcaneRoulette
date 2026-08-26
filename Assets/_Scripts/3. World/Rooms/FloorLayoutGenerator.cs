using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace World
{
    /// <summary>
    /// A single generated room slot, before physical spawning.
    /// </summary>
    public sealed class RoomLayoutNode
    {
        public int Index { get; }
        public Vector2Int GridPosition { get; }
        public RoomType Type { get; set; }
        public List<int> NeighborIndices { get; } = new List<int>();

        public RoomLayoutNode(int index, Vector2Int gridPosition)
        {
            Index = index;
            GridPosition = gridPosition;
        }
    }

    /// <summary>
    /// Builds a connected grid graph of rooms and assigns types per the Floor Generation
    /// Feature Design Document. No UnityEngine dependency beyond Vector2Int, so this is
    /// directly unit-testable without a scene.
    /// </summary>
    public static class FloorLayoutGenerator
    {
        private const int MaxAttempts = 20; // bounded retry — Procedural Generation Safety

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private sealed class GenerationFailure : Exception
        {
            public GenerationFailure(string message) : base(message) { }
        }

        public static IReadOnlyList<RoomLayoutNode> Generate(ZoneDefinitionSO zone, bool isBossFloor, System.Random rng)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                try
                {
                    return TryGenerate(zone, isBossFloor, rng);
                }
                catch (GenerationFailure)
                {
                    // Progressive Constraint Relaxation: re-roll the walk. Grid growth is
                    // sub-millisecond at these room counts, so retries cost nothing.
                }
            }

            Debug.LogError($"FloorLayoutGenerator: failed for zone '{zone.name}' after {MaxAttempts} " +
                            "attempts. Check its room/combat counts for an impossible configuration.");
            return null;
        }

        public static (int min, int max) ClampCombatRange(int totalRooms, int combatMin, int combatMax)
        {
            // Mandatory non-combat slots are always Start + guaranteed Rest + (Boss or
            // Portal) = 3. Combat can never eat into those.
            int maxFeasible = Mathf.Max(0, totalRooms - 3);
            int clampedMax = Mathf.Min(combatMax, maxFeasible);
            int clampedMin = Mathf.Min(combatMin, clampedMax);
            return (clampedMin, clampedMax);
        }

        private static IReadOnlyList<RoomLayoutNode> TryGenerate(ZoneDefinitionSO zone, bool isBossFloor, System.Random rng)
        {
            int totalRooms = zone.TotalRoomsPerFloor;
            var (combatMin, combatMax) = ClampCombatRange(totalRooms, zone.CombatRoomMin, zone.CombatRoomMax);

            var nodesByPosition = new Dictionary<Vector2Int, RoomLayoutNode>();
            var nodesByIndex = new List<RoomLayoutNode>();

            var start = new RoomLayoutNode(0, Vector2Int.zero) { Type = RoomType.Start };
            nodesByPosition[Vector2Int.zero] = start;
            nodesByIndex.Add(start);

            // Force Start to branch twice immediately so it can never end up a leaf —
            // guarantees "Start cannot be a leaf" by construction instead of by retry.
            List<Vector2Int> startFrontier = EmptyNeighbors(Vector2Int.zero, nodesByPosition);
            Shuffle(startFrontier, rng);
            if (startFrontier.Count < 2)
                throw new GenerationFailure("Start cell has fewer than 2 open directions.");

            for (int i = 0; i < 2; i++)
            {
                var node = new RoomLayoutNode(nodesByIndex.Count, startFrontier[i]);
                nodesByPosition[startFrontier[i]] = node;
                nodesByIndex.Add(node);
                Connect(start, node);
            }

            var frontier = new HashSet<Vector2Int>();
            foreach (var node in nodesByIndex)
                foreach (var c in EmptyNeighbors(node.GridPosition, nodesByPosition))
                    frontier.Add(c);

            while (nodesByIndex.Count < totalRooms)
            {
                if (frontier.Count == 0)
                    throw new GenerationFailure("Frontier exhausted before reaching room count.");

                Vector2Int cell = frontier.ElementAt(rng.Next(frontier.Count));
                frontier.Remove(cell);

                List<RoomLayoutNode> occupiedNeighbors = OccupiedNeighbors(cell, nodesByPosition);
                if (occupiedNeighbors.Count == 0)
                    continue; // defensive only — shouldn't be reachable given how frontier is built

                var newNode = new RoomLayoutNode(nodesByIndex.Count, cell);
                nodesByPosition[cell] = newNode;
                nodesByIndex.Add(newNode);

                foreach (RoomLayoutNode source in occupiedNeighbors)
                {
                    Connect(source, newNode);
                }
            }

            // ---- Assign mandatory rooms ----
            var assigned = new HashSet<int> { 0 };
            List<RoomLayoutNode> leaves = nodesByIndex
                .Where(n => n.Index != 0 && n.NeighborIndices.Count == 1)
                .ToList();
            Shuffle(leaves, rng);
            if (leaves.Count == 0)
                throw new GenerationFailure("No eligible leaf for Boss/Portal.");

            if (isBossFloor && zone.HasBossEncounter)
            {
                RoomLayoutNode boss = leaves[leaves.Count - 1];
                boss.Type = RoomType.Boss;
                assigned.Add(boss.Index);
            }
            else
            {
                RoomLayoutNode portal = null;
                foreach (var candidate in leaves)
                {
                    int neighbor = candidate.NeighborIndices[0];
                    if (!assigned.Contains(neighbor))
                    {
                        portal = candidate;
                        break;
                    }
                }
                if (portal == null)
                    throw new GenerationFailure("No leaf available whose neighbor is free for Portal.");

                portal.Type = RoomType.Portal;
                assigned.Add(portal.Index);

                // Design doc: "The Portal Room must always be connected to a Combat Room."
                RoomLayoutNode forcedCombat = nodesByIndex[portal.NeighborIndices[0]];
                forcedCombat.Type = RoomType.Combat;
                assigned.Add(forcedCombat.Index);
            }

            // Guaranteed Rest Room — outside the weighted pool.
            List<RoomLayoutNode> remaining = nodesByIndex.Where(n => !assigned.Contains(n.Index)).ToList();
            if (remaining.Count == 0)
                throw new GenerationFailure("No room left for the guaranteed Rest Room.");
            RoomLayoutNode rest = remaining[rng.Next(remaining.Count)];
            rest.Type = RoomType.Resting;
            assigned.Add(rest.Index);

            // Combat Rooms — roll within the (clamped) zone range, minus whatever Portal already forced.
            int combatTarget = rng.Next(combatMin, combatMax + 1); // Next's upper bound is exclusive
            int alreadyPlacedCombat = isBossFloor ? 0 : 1;
            remaining = nodesByIndex.Where(n => !assigned.Contains(n.Index)).ToList();
            int combatToPlace = Math.Max(0, Math.Min(combatTarget - alreadyPlacedCombat, remaining.Count));
            Shuffle(remaining, rng);
            for (int i = 0; i < combatToPlace; i++)
            {
                remaining[i].Type = RoomType.Combat;
                assigned.Add(remaining[i].Index);
            }

            // Everything left comes from the weighted Non-Combat pool.
            var weightManager = new RoomWeightManager(zone.StartingRoomWeight, zone.WeightPenalty, zone.WeightBonus);
            remaining = nodesByIndex.Where(n => !assigned.Contains(n.Index)).ToList();
            foreach (var node in remaining)
            {
                node.Type = weightManager.GetNextRoom();
                assigned.Add(node.Index);
            }

            return nodesByIndex;
        }

        private static void Connect(RoomLayoutNode a, RoomLayoutNode b)
        {
            a.NeighborIndices.Add(b.Index);
            b.NeighborIndices.Add(a.Index);
        }

        private static List<Vector2Int> EmptyNeighbors(Vector2Int cell, Dictionary<Vector2Int, RoomLayoutNode> occupied)
        {
            var result = new List<Vector2Int>(4);
            foreach (var dir in Directions)
                if (!occupied.ContainsKey(cell + dir))
                    result.Add(cell + dir);
            return result;
        }

        private static List<RoomLayoutNode> OccupiedNeighbors(Vector2Int cell, Dictionary<Vector2Int, RoomLayoutNode> occupied)
        {
            var result = new List<RoomLayoutNode>(4);
            foreach (var dir in Directions)
                if (occupied.TryGetValue(cell + dir, out var node))
                    result.Add(node);
            return result;
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}