using UnityEngine;

namespace World
{
    [CreateAssetMenu(fileName = "MeleeEnemyStats", menuName = "World/Enemies/Melee Stats")]
    public class MeleeEnemyStats : EnemyStats
    {
        [Header("Spawn")]
        [Tooltip("Seconds of spawn animation before the enemy starts acting.")]
        public float SpawnDuration = 3f;

        [Header("Attacking — shared")]
        [Tooltip("Max degrees the enemy may redirect toward the player between attacks 1->2 and 2->3.")]
        public float MaxRedirectAngle = 45f;
        public float WindupDuration = 0.5f;
        [Tooltip("Percentage of Movement Speed the enemy is slowed to during windup (0-1). Not yet wired to movement — see chat.")]
        [Range(0f, 1f)]
        public float WindupSlowPercentage = 0.5f;

        [Header("Attack 1 (Attack 2 is identical per the FDD, only its End Delay differs)")]
        [Tooltip("Forward-step speed during the swing, as a fraction of Movement Speed.")]
        [Range(0f, 1f)]
        public float Attack1MovementSpeedMultiplier = 0.2f;
        public float Attack1SwingSpeedMultiplier = 1f;
        public Vector3 Attack1HitboxSize = new Vector3(1f, 1f, 1.5f);
        [Range(0f, 1f)]
        public float Attack1DamageMultiplier = 0.5f;
        public float Attack1EndDelay = 0.1f;

        [Header("Attack 2")]
        public float Attack2EndDelay = 0.2f;

        [Header("Attack 3")]
        public float Attack3SwingSpeedMultiplier = 1f;
        public float Attack3DashSpeedMultiplier = 1f;
        public float Attack3DashDistance = 3f;
        [Tooltip("Extra hitbox size in the direction of the sword tip (0-1, e.g. 0.15 = 15%).")]
        public float Attack3HitboxSizeMultiplier = 0.15f;
        [Range(0f, 1f)]
        public float Attack3DamageMultiplier = 1f;

        [Header("Recomposing")]
        public float RecomposingDuration = 2f;
    }
}