using System.Collections.Generic;

namespace Foundation
{
    public static class OcclusionRegistry
    {
        public static readonly HashSet<IOccluder> Occluders = new();
        
        public static void Register(IOccluder occluder) => Occluders.Add(occluder);
        public static void Unregister(IOccluder occluder) => Occluders.Remove(occluder);
    }
}