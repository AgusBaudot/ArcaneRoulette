using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Global Enemy Settings")]
        public int BaseContactDamage = 2;
        
        [Header("Damage Juice Presets")]
        public DamageJuice PlayerDamage;

        public DamageJuice BigDMG; //Had 0.15f
        public DamageJuice NormalDMG;
        public DamageJuice SmallDMG;
        public DamageJuice NoFeedback;
        public DamageJuice BombExplosion;
    }
}