using UnityEngine;

namespace Foundation
{
    public abstract class RuneDefinitionSO : ScriptableObject
    {
        public int ID;
        public string Name;
        public string Type;
        public Sprite Icon;
        public virtual float UIIconScale => 1.0f;
        public Sprite StoneLessIcon;
        [TextArea]
        public string Description;
    }
}