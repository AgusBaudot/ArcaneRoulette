using UnityEngine;

namespace Foundation
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Element")]
    public class ElementRuneSO : RuneDefinitionSO
    {
        public ElementType Element;
    }
}