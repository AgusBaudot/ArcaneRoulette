using UnityEngine;

namespace Foundation
{
    public interface ISpellSource
    {
        ElementType SpellElement { get; }
        ShieldInstanceState ShieldState { get; } //null for non-hold spells.
        
        void TriggerOnHit(Vector3 position, GameObject target, MonoBehaviour runner);
        void TriggerOnHit(Vector3 position, GameObject target, MonoBehaviour runner, 
            AbilityType abilityTypeForContext, bool excludeBounceCastRune, Vector3 attackerDirection = default);
    }
}