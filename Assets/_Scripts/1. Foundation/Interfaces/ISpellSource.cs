using UnityEngine;

namespace Foundation
{
    public interface ISpellSource
    {
        ElementType SpellElement { get; }
        
        void TriggerOnHit(Vector3 position, GameObject target, MonoBehaviour runner);

        void TriggerOnHit(Vector3 position, GameObject target, MonoBehaviour runner, 
            AbilityType abilityTypeForContext, bool excludeBounceCastRune, Vector3 attackerDirection = default);
    }
}