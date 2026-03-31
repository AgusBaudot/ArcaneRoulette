using UnityEngine;

namespace Core
{
    /// <summary>
    /// Distributes count directions evenly across totalSpreadDeg,
    /// centered on baseDir, on the XZ plane.
    /// count = 1 -> single direction (straight)
    /// count = 2 -> ±spread/2
    /// count = n -> evenly from -spread/2 to +spread/2
    /// </summary>
    public static class ReflectionUtils
    {
        public static Vector3[] GetSpreadDirections(Vector3 baseDir, int count, float totalSpreadDeg)
        {
            baseDir.y = 0f;
            baseDir.Normalize();

            if (count <= 1)
                return new[] { baseDir };
            
            //Avoid calculating in each iteration
            var dirs = new Vector3[count];
            float step = totalSpreadDeg / (count - 1);
            float start = -totalSpreadDeg / 2f;

            for (int i = 0; i < count; i++)
                dirs[i] = Quaternion.AngleAxis(start + step * i, Vector3.up) * baseDir;
            
            return dirs;
        }
    }
}