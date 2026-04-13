using UnityEngine;

namespace UI
{
    [CreateAssetMenu(menuName = "ArcaneRoulette/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string ItemName;
        public Sprite Icon;
        public GameObject WorldPrefab;
        public bool Stackable = true;
        public int MaxStack = 99;
        public string ID;
    }
}
