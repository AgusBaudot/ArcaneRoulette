using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public sealed class AIMelee : AIBrain
    {
        [Header("Melee Settings")]
        [SerializeField] private float exitAttackRange; // it must always be greater than _attackRange
        private bool _wasInRange;

        [Header("Prefab")]
        [SerializeField] private HitEffect _hitEffect;

        [Header("Hit")]
        [SerializeField] private float _attackRadius;
        [SerializeField] private LayerMask _playerLayer;
        protected override void Awake()
        {
            base.Awake();
        }

        bool IsInAttackRangeStable()
        {
            float distance = Vector3.Distance(transform.position, target.position);
            bool result;
            if (_wasInRange)
                result = distance <= _enemyStats.ExitAttackRange;
            else
                result = distance <= _enemyStats.AttackRange;
            _wasInRange = result;
            return result;
        }
        protected override BehaviourTree BuildTree() 
        {
            var tree = new BehaviourTree(base._behaviourTreeName);
            var root = new PrioritySelectorNode("Root");

            // --- Attack Sequence ---
            var attackSequence = new SequenceNode("Attack", 2);
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInAttackRangeStable())));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _enemyStats.AttackSpeed, "MeleePHAnim")));
            //attackSequence.AddChild(new LeafNode("wait", new Wait(_enemyStats.AttackSpeed)));

            // --- Chase ---
            var chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, transform, _agent, _enemyStats.ChaseSpeed)));

            // --- Patrol ---
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, _enemyStats.PatrolSpeed), 0);

            // --- Estructura ---
            root.AddChild(attackSequence);
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }
        public void DoHitAttack()
        {
            Vector3 dir = (target.position - transform.position).normalized;
            Vector3 pos = transform.position + dir * 1f;
            var explosion = Instantiate(_hitEffect, pos, Quaternion.identity);
            explosion.Init(_attackRadius, _enemyStats.AttackDamage, _playerLayer, target, 0.2f);
        }
    }
}

