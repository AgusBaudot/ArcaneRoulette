using UnityEngine;

namespace Foundation
{
    public interface IOccluder
    {
        Vector3 Position { get; }
        Renderer Renderer { get; }
    }
}