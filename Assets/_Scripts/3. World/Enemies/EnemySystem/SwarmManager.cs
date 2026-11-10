using System.Collections.Generic;
using UnityEngine;

namespace World
{
    /// <summary>
    /// 16 slots around the player: 8 compass slots (ring 1) + 8 half-step slots on a wider ring (ring 2).
    /// Enemies claim the free slot closest to the side they are approaching from, and switch if the
    /// player circles around so their slot ends up on the far side.
    /// GetOrClaimSlot returns a direction scaled for its ring (1.0 / 1.8); the caller multiplies by its formation radius.
    /// </summary>
    public static class SwarmManager
    {
        private const int RingSize = 8;
        private const int SlotCount = RingSize * 2;
        private const float OuterRingScale = 1.8f;
        private const float OuterRingPenalty = 0.3f;
        private const float KeepSlotMinDot = -0.2f;
        private const float SwapMinDot = 0.5f;
        private const int Free = int.MinValue;

        private static readonly Vector3[] _slots = new Vector3[SlotCount];
        private static readonly Vector3[] _slotDirs = new Vector3[SlotCount];
        private static readonly int[] _owners = new int[SlotCount];
        private static readonly Dictionary<int, int> _assignments = new Dictionary<int, int>();

        static SwarmManager()
        {
            for (int i = 0; i < RingSize; i++)
            {
                float a0 = i * 45f * Mathf.Deg2Rad;
                float a1 = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                _slotDirs[i] = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
                _slotDirs[RingSize + i] = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
                _slots[i] = _slotDirs[i];
                _slots[RingSize + i] = _slotDirs[RingSize + i] * OuterRingScale;
            }
            for (int i = 0; i < SlotCount; i++) _owners[i] = Free;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ClearAll();

        public static Vector3 GetOrClaimSlot(int enemyId, Vector3 enemyPos, Vector3 playerPos)
        {
            Vector3 bearing = enemyPos - playerPos;
            bearing.y = 0f;
            bearing = bearing.sqrMagnitude > 0.0001f ? bearing.normalized : Vector3.zero;

            if (_assignments.TryGetValue(enemyId, out int current))
            {
                if (Vector3.Dot(_slotDirs[current], bearing) > KeepSlotMinDot)
                    return _slots[current];

                int better = FindBestFreeSlot(bearing, SwapMinDot);
                if (better < 0) return _slots[current];

                ReleaseSlot(enemyId);
                Claim(enemyId, better);
                return _slots[better];
            }

            int best = FindBestFreeSlot(bearing, float.NegativeInfinity);
            if (best >= 0)
            {
                Claim(enemyId, best);
                return _slots[best];
            }

            int shared = FindBestSlotIgnoringOwners(bearing);
            _assignments[enemyId] = shared;
            return _slots[shared];
        }

        public static void ReleaseSlot(int enemyId)
        {
            if (_assignments.TryGetValue(enemyId, out int slot))
            {
                if (_owners[slot] == enemyId) _owners[slot] = Free;
                _assignments.Remove(enemyId);
            }
        }

        public static void ClearAll() 
        {
            _assignments.Clear();
            for (int i = 0; i < SlotCount; i++) _owners[i] = Free;
        }

        private static void Claim(int enemyId, int slot)
        {
            _owners[slot] = enemyId;
            _assignments[enemyId] = slot;
        }

        private static int FindBestFreeSlot(Vector3 bearing, float minDot)
        {
            int best = -1;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < SlotCount; i++)
            {
                if (_owners[i] != Free) continue;
                float dot = Vector3.Dot(_slotDirs[i], bearing);
                if (dot < minDot) continue;
                float score = dot - (i >= RingSize ? OuterRingPenalty : 0f);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        private static int FindBestSlotIgnoringOwners(Vector3 bearing)
        {
            int best = 0;
            float bestDot = float.NegativeInfinity;
            for (int i = 0; i < SlotCount; i++)
            {
                float dot = Vector3.Dot(_slotDirs[i], bearing);
                if (dot > bestDot) { bestDot = dot; best = i; }
            }
            return best;
        }
    }
}