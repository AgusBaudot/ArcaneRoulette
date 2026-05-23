using Foundation;
using UnityEngine;

namespace World 
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "World/Enemies/Stats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")]
        [SerializeField] public float MaxHp;
        [SerializeField] public ElementType ElementType;

        [Header("Movement")]
        [SerializeField] public float ChaseSpeed;
        [SerializeField] public float PatrolSpeed;

        [Header("Combat")]
        [SerializeField] public float AttackDamage;
        [SerializeField] public float AttackRange;
        [SerializeField] public float AttackSpeed;
    }
}
