using UnityEngine;

namespace Foundation
{
    public abstract class RuneDefinitionSO : ScriptableObject
    {
        public int ID;
        public string Name;
        public string Type;
        public Sprite Icon;
        public Sprite StoneLessIcon;
        [TextArea]
        public string Description;
    }
}