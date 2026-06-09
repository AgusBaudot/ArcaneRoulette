using Foundation;
using UnityEngine;

namespace World
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "World/Enemies/Stats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")]
        [Tooltip("Max Health.")]
        public float MaxHp;
        [Tooltip("Enemy Element Type.")]
        public ElementType ElementType;

        [Header("Sense")]
        [Tooltip("How far can the enemy see.")]
        public float viewDistance;
        [Tooltip("Wich layer is blocking the view.")]
        public LayerMask obsMask;

        [Header("Movement")]
        [Tooltip("The speed at which the enemy moves when is chasing.")]
        public float ChaseSpeed;
        [Tooltip("The speed at which the enemy moves when is not seeing the player.")]
        public float PatrolSpeed;

        [Header("Combat")]
        [Tooltip("Base Attack Damage.")]
        public float AttackDamage;
        [Tooltip("How far away must this enemy be from the player to attack?")]
        public float AttackRange;
        [Tooltip("When the enemy enters combat range: What is the new distance at which to cease combat? (It must be at least 0.1 greater than Attack Range)")]
        public float ExitAttackRange;
        [Tooltip("Interval between each attacks. (lower value = faster)")] // Cuando tengamos animaciones esto debe cambiar
        public float AttackSpeed;
    }
}
