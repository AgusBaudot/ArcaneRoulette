using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Global Enemy Settings")]
        public int BaseContactDamage = 2;
    }
}