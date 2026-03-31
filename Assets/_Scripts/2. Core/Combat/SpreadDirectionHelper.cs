using UnityEngine;

namespace Core
{
    public static class SpreadDirectionHelper
    {
        /// <summary>
        /// Distributes count directions evenly across totalSpreadDeg,
        /// centered on baseDir, on the XZ plane.
        /// count = 1 -> single direction (straight)
        /// count = 2 -> ±spread/2
        /// count = n -> evenly from -spread/2 to +spread/2
        /// </summary>
        /// <param name="baseDir"></param>
        /// <param name="count"></param>
        /// <param name="totalSpreadDeg"></param>
        /// <returns></returns>
        public static Vector3[] GetSpreadDirection(
            Vector3 baseDir, int count, float totalSpreadDeg)
        {
            baseDir.y = 0;
            baseDir.Normalize();

            if (count <= 1)
                return new[] { baseDir };
            
            //Initialize before iteration
            var dirs = new Vector3[count];
            float step = totalSpreadDeg / (count - 1);
            float start = -totalSpreadDeg / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = start + step * i;
                dirs[i] = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;
            }

            return dirs;
        }
    }
}