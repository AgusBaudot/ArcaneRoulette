using System.Collections.Generic;

namespace Foundation
{
    public static class OcclusionRegistry
    {
        public static readonly HashSet<IOcclusionTarget> Targets = new();
        
        public static void Register(IOcclusionTarget target) => Targets.Add(target);
        public static void Unregister(IOcclusionTarget target) => Targets.Remove(target);
    }
}