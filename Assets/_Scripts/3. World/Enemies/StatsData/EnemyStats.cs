using Foundation;
using UnityEngine;

namespace World
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "World/Enemies/Stats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")]
        [Tooltip("Max Health.")]
        [SerializeField, Range(1, 20)]
        public float MaxHp;
        [Tooltip("Enemy Element Type.")]
        public ElementType ElementType;

        [Header("Sense")]
        [Tooltip("How far can the enemy see.")]
        [SerializeField, Range(3, 35)]
        public float ViewDistance;
        [Tooltip("Wich layer is blocking the view.(NOT IMPLEMENTED)")]
        public LayerMask ObsMask;

        [Header("Movement")]
        [Tooltip("The speed at which the enemy moves when is chasing.")]
        [SerializeField, Range(2, 15)]
        public float ChaseSpeed;
        [Tooltip("The speed at which the enemy moves when is not seeing the player.")]
        [SerializeField, Range(1, 14)]
        public float PatrolSpeed;
        [Tooltip("If this enemy apply Fleeing Behaviour then How fast is this enemy while fleeing?")]
        [SerializeField, Range(0, 15)]
        public float FleeSpeed;

        [Header("Flee Bounds")]
        [Tooltip("How far away must this enemy be from the player to attack?")]
        [SerializeField, Range(0, 10)]
        public float DangerRange;
        [Tooltip("When the enemy enters combat range: What is the new distance at which to cease combat? (It must be at least 0.1 greater than Attack Range)")]
        [HideInInspector]
        public float ExitDangerRange;   

        [Header("Combat")]
        [Tooltip("Base Attack Damage.")]
        public float AttackDamage;
        [Tooltip("How far away must this enemy be from the player to attack?")]
        [SerializeField, Range(1, 30)]
        public float AttackRange;
        [Tooltip("When the enemy enters combat range: What is the new distance at which to cease combat? (It must be at least 0.1 greater than Attack Range)")]
        [HideInInspector]        
        public float ExitAttackRange;
        [Tooltip("Each enemy has an attack radius, if this enemy does not have an attack radius, the recommended value is 1.)")]
        public float AttackRadius;
        [Tooltip("Interval between each attacks. (lower value = faster)")] // Cuando tengamos animaciones esto debe cambiar
        public float AttackSpeed;
        [Tooltip("The player layer or any layer that this enemy can hit.")]
        public LayerMask HitLayer;

        public void OnValidate()
        {
            ExitAttackRange = AttackRange + 2;
            ExitDangerRange = DangerRange + 2;

            ViewDistance = Mathf.Max(ViewDistance, AttackRange + 1);

            if (ChaseSpeed < PatrolSpeed + 1)
                ChaseSpeed = PatrolSpeed + 1;

            DangerRange = Mathf.Clamp(DangerRange, 0, AttackRange - 1);
        }
    }
}
