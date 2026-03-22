using UnityEngine;

namespace Foundation
{
    public abstract class RuneDefinitionSO : ScriptableObject
    {
        public int ID;
        public string Name;
        public Sprite Icon;
        public string Description;
    }
}