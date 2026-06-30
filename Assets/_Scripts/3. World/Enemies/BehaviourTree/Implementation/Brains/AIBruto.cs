using UnityEngine;

namespace World
{
    public class AIBruto : AIBrain
    {
        #region SerializeField
        [Header("Bruto Config")]
        [SerializeField] private ExplosionArea _explosionAreaPrefab;
        #endregion
        private bool _wasInAttackRange;
        private bool IsInAttackRange() => IsInStableDistance(_player, _enemyStats.AttackRange, _enemyStats.ExitAttackRange, ref _wasInAttackRange);
        public override void ResetComponent()
        {
            base.ResetComponent();
            _wasInAttackRange = false;
        }
        protected override void Awake()
        {
            base.Awake();
        }
        protected override BehaviorTree BuildTree()
        {
            BehaviorTree tree = new BehaviorTree(base._behaviourTreeName);
            PrioritySelectorNode root = new PrioritySelectorNode("Root");

            // --- Attack Sequence ---
            SequenceNode attackSequence = new SequenceNode("Attack", 2);
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(IsInAttackRange)));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _agent, () => EffectiveAttackSpeed, "BrutoPHAnim")));

            // --- Chase ---
            SequenceNode chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(_player, transform, _agent, () => EffectiveChaseSpeed, _enemyStats.AttackRange)));

            // --- Patrol ---
            LeafNode patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, _enemyStats.PatrolSpeed), 0);

            // --- Structure ---
            root.AddChild(attackSequence);
            root.AddChild(chaseSequence);
            root.AddChild(patrol);
            tree.AddChild(root);
            return tree;
        }
        public void DoAreaAttack()
        {
            Vector3 dir = (_player.position - transform.position).normalized;
            Vector3 pos = transform.position + dir * 3f;
            ExplosionArea explosion = Instantiate(_explosionAreaPrefab, pos, Quaternion.identity);
            explosion.Init(_enemyStats.AttackRadius, EffectiveAttackDamage, _enemyStats.HitLayer, _player, 1);
        }
    }
}

