using Foundation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    [Serializable]
    public struct ElementalAffinity
    {
        public ElementType Element;
        public Effectiveness Effectiveness;
    }

    public class ElementalResistanceComponent : MonoBehaviour, IElementalResistance
    {
        [Tooltip("Define specific weaknesses/resistances. Unlisted elements default to Neutral.")]
        [SerializeField] private List<ElementalAffinity> _affinities = new();

        //Optional: Cache in dictionary for faster lookups at runtime O(1)
        private Dictionary<ElementType, Effectiveness> _affinityMap = new();

        private void Awake()
        {
            foreach (var affinity in _affinities)
            {
                _affinityMap[affinity.Element] = affinity.Effectiveness;
            }
        }

        public Effectiveness GetEffectiveness(ElementType attackerElement)
        {
            if (_affinityMap.TryGetValue(attackerElement, out Effectiveness effectiveness))
            {
                return effectiveness;
            }

            //Default to Neutral if the designer didn't explicitly map this element
            return Effectiveness.Neutral;
        }
    } 
}
