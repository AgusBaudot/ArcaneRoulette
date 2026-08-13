using UnityEngine;

namespace World
{
    [CreateAssetMenu(fileName = "BruteEnemyStats", menuName = "Stats/Enemies/Brute")]
    public class BruteEnemyStats : EnemyStats
    {
        [Header("Brute - General")]
        public float SpawnDuration = 3f;

        [Header("Brute - Ranges")]
        public float AoEAttackRange = 3f;
        public float ChargeAttackRange = 8f;
        public float DefenseTargetLineRatio = 0.5f; // 0.5 = exact middle

        [Header("Brute - AoE Thrust")]
        public float ThrustHitboxMaxDistance = 5f;
        public float ThrustWindupDuration = 2f;
        public float ThrustRecomposeDuration = 1f;

        [Header("Brute - Charge")]
        public float ChargeWindupDuration = 2f;
        public float ChargeCooldown = 20f;
        public float ChargeSpeedMultiplier = 2f; 
        public float ChargeDamagePercentage = 1f; 

        [Header("Brute - Stun Durations")]
        public float StunDurationBase = 2f; 
        public float StunDurationImpact = 4f; 
        public float StunDurationShield = 6f; 
        public float StunDurationBombBonus = 2f;
    }
}