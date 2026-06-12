using UnityEngine;

namespace World 
{
    public sealed class AIMelee : AIBrain
    {
        #region SerializeField
        [Header("Melee Config")]
        [SerializeField] private HitEffect _hitEffect;
        #endregion
        protected override void Awake()
        {
            base.Awake();
        }
        protected override BehaviourTree BuildTree() 
        {
            BehaviourTree tree = new BehaviourTree(base._behaviourTreeName);
            PrioritySelectorNode root = new PrioritySelectorNode("Root");

            // --- Attack Sequence ---
            SequenceNode attackSequence = new SequenceNode("Attack", 2);
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInAttackRangeStable())));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _agent,() => EffectiveAttackSpeed, "MeleePHAnim")));
            //attackSequence.AddChild(new LeafNode("wait", new Wait(_enemyStats.AttackSpeed)));

            // --- Chase ---
            SequenceNode chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, transform, _agent, () => EffectiveChaseSpeed)));

            // --- Patrol ---
            LeafNode patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, _enemyStats.PatrolSpeed), 0);

            // --- Structure ---
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
            explosion.Init(_enemyStats.AttackRadius, EffectiveAttackDamage, _enemyStats.HitLayer, target, 0.2f);
        }
    }
}

