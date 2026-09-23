using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public static class SwarmManager
    {
        private static readonly Vector3[] _slots = new Vector3[]
        {
            new Vector3( 0, 0,  1), new Vector3( 1, 0,  1).normalized, // N, NE
            new Vector3( 1, 0,  0), new Vector3( 1, 0, -1).normalized, // E, SE
            new Vector3( 0, 0, -1), new Vector3(-1, 0, -1).normalized, // S, SW
            new Vector3(-1, 0,  0), new Vector3(-1, 0,  1).normalized  // W, NW
        };

        private static readonly Dictionary<int, int> _assignments = new Dictionary<int, int>();

        public static Vector3 GetOrClaimSlot(int enemyId)
        {
            if (_assignments.TryGetValue(enemyId, out int slotIndex))
            {
                return _slots[slotIndex];
            }

            for (int i = 0; i < 8; i++)
            {
                if (!_assignments.ContainsValue(i))
                {
                    _assignments[enemyId] = i;
                    return _slots[i];
                }
            }

            int randomSlot = Random.Range(0, 8);
            _assignments[enemyId] = randomSlot;
            return _slots[randomSlot];
        }

        public static void ReleaseSlot(int enemyId)
        {
            _assignments.Remove(enemyId);
        }

        public static void ClearAll() 
        {
            _assignments.Clear();
        }
    }
}