using System;
using UnityEngine;
using Foundation;

namespace Core
{
    /// <summary>
    /// Maps an element to a GameObject. 
    /// Can be used for Prefab references (Shields) or child instance references (Projectile visuals).
    /// </summary>
    [Serializable]
    public struct ElementalGameObject
    {
        public ElementType Element;
        public GameObject Reference;
    }

    /// <summary>
    /// Maps an element to a specific PooledVFX component.
    /// </summary>
    [Serializable]
    public struct ElementalPooledVFX
    {
        public ElementType Element;
        public PooledVFX Prefab;
    }
}