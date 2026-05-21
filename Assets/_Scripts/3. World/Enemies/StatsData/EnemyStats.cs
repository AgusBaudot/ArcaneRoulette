using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace World 
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "World/Enemies/Stats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")]
        [SerializeField] private float _maxHp;
        [SerializeField] private ElementType _type;

        [Header("Movement")]
        [SerializeField] private float _chaseSpeed;
        [SerializeField] private float _patrolSpeed;

        [Header("Combat")]
        [SerializeField] private float _attackDamage;
        [SerializeField] private float _attackRange;
        [SerializeField] private float _attackSpeed;

        public float MaxHp => _maxHp;
        public ElementType Type => _type;
        public float ChaseSpeed => _chaseSpeed;
        public float PatrolSpeed => _patrolSpeed;
        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float AttackSpeed => _attackSpeed;
    }
}
