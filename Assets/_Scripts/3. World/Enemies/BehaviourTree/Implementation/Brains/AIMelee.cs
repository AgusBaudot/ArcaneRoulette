using UnityEngine;

namespace World 
{
    public sealed class AIMelee : AIBrain
    {
        [Header("Melee Internal values")]
        [SerializeField] private HitEffect _hitEffect;
        [SerializeField] private float _attackRadius;
        [SerializeField] private LayerMask _playerLayer;
        protected override void Awake()
        {
            base.Awake();
        }
        protected override BehaviourTree BuildTree() 
        {
            var tree = new BehaviourTree(base._behaviourTreeName);
            var root = new PrioritySelectorNode("Root");

            // --- Attack Sequence ---
            var attackSequence = new SequenceNode("Attack", 2);
            attackSequence.AddChild(new LeafNode("ApplyDebuff", new ConditionNode(() => ApplyDebuff())));
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInAttackRangeStable())));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, () => _currentAttackSpeed, "MeleePHAnim")));
            //attackSequence.AddChild(new LeafNode("wait", new Wait(_enemyStats.AttackSpeed)));

            // --- Chase ---
            var chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, transform, _agent, () => _currentChaseSpeed)));

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
            explosion.Init(_attackRadius, _currentAttackDamage, _playerLayer, target, 0.2f);
        }
    }
}

