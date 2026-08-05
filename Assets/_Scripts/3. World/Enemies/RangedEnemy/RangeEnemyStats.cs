using UnityEngine;

namespace World
{
    [CreateAssetMenu(fileName = "RangeEnemyStats", menuName = "World/Enemies/Range Stats")]
    public class RangeEnemyStats : EnemyStats
    {
        [Header("Spawn & Behavior")]
        public float SpawnDuration = 3f;
        [Tooltip("The distance at which the enemy will consider blocking or teleporting.")]
        public float SafeRange = 4f;
        
        [Header("Attack Speeds & Clamps")]
        [Tooltip("Hard cap on windup durations to prevent extreme slow-motion from debuffs.")]
        public float MaxWindupDuration = 2.0f;
        public float Attack1Windup = 1.0f;
        public float Attack2Windup = 0.3f;
        public float Attack3Windup = 0.3f;
        public float Attack4Windup = 1.0f;

        [Header("Prediction (Projectiles 2 & 3)")]
        public float PredictionTime2 = 0.5f;
        public float PredictionTime3 = 1.5f;

        [Header("Projectiles")]
        [Tooltip("Prefab for the Normal Projectile")]
        public EnemyProjectile NormalProjectilePrefab; // FIXED: Strictly typed
        
        [Tooltip("Prefab for the Big Projectile")]
        public DetonatingEnemyProjectile BigProjectilePrefab; // FIXED: Strictly typed
        
        public float NormalProjectileSpeed = 12f;
        public float BigProjectileInitialSpeed = 8f;
        public float BigProjectileDrainRate = 1.5f;

        [Header("Defense & Teleport")]
        public float BlockCoverDuration = 1f;
        public float BlockUncoverDuration = 1f;
        public float BlockTimeout = 3f;
        public float TeleportAnimDuration = 4f;
    }
}