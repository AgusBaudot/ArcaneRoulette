using System.Linq;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Global Enemy Settings")]
        public int BaseContactDamage = 2;
        
        [Header("Projectile Impacts")]
        [SerializeField] private PooledVFX _enemyProjectileImpact;
        [SerializeField] private ElementalPooledVFX[] _playerProjectileImpacts;
        
        [Header("Projectile Impact Audio")]
        [SerializeField] private AudioEventSO _defaultEnemyImpactSound;
        [SerializeField] private ElementalImpactAudio[] _playerElementalImpactSounds;

        public PooledVFX GetImpactVFX(bool isEnemy, ElementType element)
        {
            if (isEnemy && _enemyProjectileImpact != null)
                return _enemyProjectileImpact;

            return (
                from elementalVFX in _playerProjectileImpacts 
                where elementalVFX.Element == element 
                select elementalVFX.Prefab)
                .FirstOrDefault();
        }

        public AudioEventSO GetProjectileImpactSound(bool isEnemy, ElementType element)
        {
            if (isEnemy)
                return _defaultEnemyImpactSound;

            foreach (var audioData in _playerElementalImpactSounds)
            {
                if (audioData.Element == element)
                    return audioData.ImpactSound;
            }

            Debug.LogWarning($"Sound for element {element} not found. Check CombatSettings SO.");
            return null;
        }
        
        [Header("Damage Juice Presets")]
        public DamageJuice PlayerDamage;
        public DamageJuice BigDMG; //Had 0.15f
        public DamageJuice NormalDMG;
        public DamageJuice SmallDMG;
        public DamageJuice NoFeedback;
        public DamageJuice BombExplosion;
    }
}