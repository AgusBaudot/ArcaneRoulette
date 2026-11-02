using UnityEngine;
using Foundation;

namespace World
{
    [CreateAssetMenu(fileName = "BruteEnemyStats", menuName = "World/Enemies/Brute Stats")]
    public class BruteEnemyStats : EnemyStats
    {
        [Header("Brute - General")]
        public float SpawnDuration = 3f;

        [Header("Brute - Ranges")]
        public float AoEAttackRange = 3f;
        public float ChargeAttackRange = 8f;
        public float DefenseTargetLineRatio = 0.5f;

        [Header("Brute - AoE Thrust")]
        public float ThrustHitboxMaxDistance = 5f;
        public float ThrustWindupDuration = 2f;
        public float ThrustDuration = 0.5f;
        public float ThrustRecomposeDuration = 1f;
        public AudioEventSO ThrustSound;

        [Header("Brute - Charge")]
        public float ChargeWindupDuration = 2f;
        public float ChargeCooldown = 20f;
        public float ChargeSpeedMultiplier = 2f; 
        public float ChargeDamagePercentage = 1f; 
        public float ChargeMissRecoverDuration = 1.0f;
        public AudioEventSO ChargeSound;
        
        [Header("Brute - Post Charge & Stun")]
        public float PostChargeDuration = 0.5f; 
        public float StunRecoverDuration = 1.0f; 
        public float StunDurationBase = 2f; 
        public float StunDurationImpact = 4f; 
        public float StunDurationShield = 6f; 
        public float StunDurationBombBonus = 2f;
    }
}