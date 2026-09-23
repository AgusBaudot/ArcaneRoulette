using UnityEngine;
namespace World 
{
    public struct DoorInfo
    {
        public bool UnlockOnClear;
        public Material Material;
        
        // Future: per-side wall variants for sides with no connection, so a designer can
        // author several "closed" dressings instead of one generic wall material. Not
        // wired up — shape depends on assets that don't exist yet.
        // public GameObject[] WallVariants;
    }
}
