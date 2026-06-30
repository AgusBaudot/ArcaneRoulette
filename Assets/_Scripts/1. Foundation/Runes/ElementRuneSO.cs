using UnityEngine;

namespace Foundation
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Element")]
    public sealed class ElementRuneSO : RuneDefinitionSO
    {
        public override float UIIconScale => 1.1f;
        
        [SerializeField] private ElementType _element;
        public ElementType Element => _element;
    }
}