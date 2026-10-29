using System;
using System.Linq;
using Foundation;
using UnityEngine;

namespace Core
{
    [Serializable]
    public struct ElementInteraction
    {
        [Tooltip("The inherent element of the target receiving damage.")]
        public ElementType TargetElement;
        
        [Tooltip("The element of the incoming attack.")]
        public ElementType AttackerElement;
        
        public Effectiveness Effectiveness;
    }

    [CreateAssetMenu(menuName = "ScriptableObjects/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Global Enemy Settings")]
        public int BaseContactDamage = 2;
        public float ContactDamageInterval = 1f;
        
        [Header("Elemental Matrix")]
        [Tooltip("Define element match-ups here. Unlisted pairs default to Neutral.")]
        [SerializeField] private ElementInteraction[] _elementInteractions;

        [Header("Effectiveness Multipliers")]
        [Tooltip("Neutral is always inherently 1.0x")]
        public float WeakMultiplier = 1.5f;
        public float ResistantMultiplier = 0.5f;
        public float ImmuneMultiplier = 0f;
        
        [Header("Projectile Impacts")]
        [SerializeField] private PooledVFX _enemyProjectileImpact;
        [SerializeField] private ElementalPooledVFX[] _playerProjectileImpacts;
        
        [Header("Projectile Impact Audio")]
        [SerializeField] private AudioEventSO _defaultEnemyImpactSound;
        [SerializeField] private ElementalImpactAudio[] _playerElementalImpactSounds;

        [Header("Projectile Hit Feedback")]
        public float BlinkTime = 0.07f;
        public float EnemyKnockback = 12f;

        [Header("Damage Juice Presets")]
        public DamageJuice PlayerDamage;
        public DamageJuice BigDMG;
        public DamageJuice NormalDMG;
        public DamageJuice SmallDMG;
        public DamageJuice NoFeedback;
        public DamageJuice BombExplosion;

        // --- Methods ---

        public Effectiveness GetEffectiveness(ElementType targetElement, ElementType attackerElement)
        {
            foreach (var interaction in _elementInteractions)
            {
                if (interaction.TargetElement == targetElement && interaction.AttackerElement == attackerElement)
                {
                    return interaction.Effectiveness;
                }
            }

            return Effectiveness.Neutral;
        }

        public float GetEffectivenessMultiplier(Effectiveness effectiveness)
        {
            return effectiveness switch
            {
                Effectiveness.Weak => WeakMultiplier,
                Effectiveness.Resistant => ResistantMultiplier,
                Effectiveness.Immune => ImmuneMultiplier,
                _ => 1f
            };
        }

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
    }
}