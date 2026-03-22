using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [Serializable]
    public class SpellRecipe
    {
        public const int MODIFIER_SLOTS = 5;
        
        [SerializeField] private AbilityRuneSO _ability;
        [SerializeField] private ElementRuneSO  _element; //Null = no element
        [SerializeField] private ModifierRuneSO[] _modifiers = new ModifierRuneSO[MODIFIER_SLOTS];
        
        public AbilityRuneSO Ability => _ability;
        public ElementRuneSO Element => _element; //Callers must null-check
        public bool HasElement => _element != null;
        public bool IsValid => _ability != null;
        
        //Typed iterators - SpellInstance uses these, never indexes _modifiers raw
        public IEnumerable<CastRuneSO> CastRunes()
        {
            foreach (var m in _modifiers)
                if (m is CastRuneSO c)
                    yield return c;
        }

        public IEnumerable<OnHitRuneSO> OnHitRunes()
        {
            foreach (var m in _modifiers)
                if (m is OnHitRuneSO h)
                    yield return h;
        }
        
        //Full modifier list for allocation counting in SpellCrafter
        public IReadOnlyList<ModifierRuneSO> Modifiers => _modifiers;
        
        //Construction
        //CraftingUI constructs this; SpellCrafter validates and produces SpellInstance.
        public SpellRecipe(AbilityRuneSO ability, ElementRuneSO element, ModifierRuneSO[] modifiers)
        {
            if (modifiers != null && modifiers.Length > MODIFIER_SLOTS)
                throw new ArgumentException($"SpellRecipe: max {MODIFIER_SLOTS} modifier slots.");
            
            _ability = ability;
            _element = element;
            _modifiers = new ModifierRuneSO[MODIFIER_SLOTS];
            
            if (modifiers != null)
                Array.Copy(modifiers, _modifiers, modifiers.Length);
        }
    }
}