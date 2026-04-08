using UnityEngine;

namespace Foundation
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Element")]
    public sealed class ElementRuneSO : RuneDefinitionSO
    {
        [SerializeField] private ElementType _element;
        public ElementType Element => _element;
    }
}