using UnityEngine;

namespace World
{
    [CreateAssetMenu(fileName = "HealerEnemyStats", menuName = "World/Enemies/Healer Stats")]
    public class HealerEnemyStats : EnemyStats
    {
        [Header("Healer - Ranges & Spacing")]
        public float HealingRange = 8f;
        public float ThrowingRange = 12f;
        public float SpacingOffset = 3f;

        [Header("Healer - Timings")]
        public float SpawnDuration = 3f;
        public float HealingDuration = 10f;
        public float HealingCooldown = 8f;
        public float ThrowingDuration = 2f;
        public float ThrowingCooldown = 15f;

        [Header("Healer - Projectile")]
        public BottleProjectile BottlePrefab;
        public float BottleThrowSpeed = 6f;
    }
}