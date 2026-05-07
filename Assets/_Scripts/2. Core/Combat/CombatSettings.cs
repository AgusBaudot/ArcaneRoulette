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
        public DamageJuice PlayerDamage = new(0.06f, 0.5f);
        public DamageJuice BigDMG = new(0.15f, 1f); //Had 0.15f
        public DamageJuice NormalDMG = new(0f, 0f);
        public DamageJuice SmallDMG = new(0f, 0f);
        public DamageJuice NoFeedback = new(0f, 0f);
        public DamageJuice BombExplosion = new(0f, 0f);
    }
}