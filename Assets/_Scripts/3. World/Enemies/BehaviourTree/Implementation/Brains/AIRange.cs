using UnityEngine;

namespace World 
{
    public class AIRange : AIBrain
    {
        #region SerializeField
        [Header("Range Prefab")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private float _projectileSpeed;
        #endregion
        private bool _wasTooClose;
        private bool _wasInAttackRange;
        private bool IsTooClose() => IsInStableDistance(_player, _enemyStats.DangerRange, _enemyStats.ExitDangerRange, ref _wasTooClose);
        private bool IsInAttackRange() => IsInStableDistance(_player, _enemyStats.AttackRange, _enemyStats.ExitAttackRange, ref _wasInAttackRange);
        protected override void Awake()
        {
            base.Awake();
        }
        public override void ResetComponent()
        {
            base.ResetComponent();
            _wasTooClose = false;
            _wasInAttackRange = false;
        }
        protected override BehaviorTree BuildTree()
        {
            BehaviorTree tree = new BehaviorTree(base._behaviourTreeName);
            PrioritySelectorNode root = new PrioritySelectorNode("Root");


            // ---- Flee ----
            SequenceNode fleeAndAttackSequence = new SequenceNode("FleeAndAttack", 3);
            fleeAndAttackSequence.AddChild(new LeafNode("IsPlayerTooClose", new ConditionNode(IsTooClose)));
            fleeAndAttackSequence.AddChild(new LeafNode("FleeAction", new Flee(_player, transform, _agent, () => EffectiveFleeSpeed, IsTooClose)));

            // --- Attack Sequence ---
            SequenceNode attackSequence = new SequenceNode("Attack", 2);
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(IsInAttackRange)));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _agent, () => EffectiveAttackSpeed, "PlaceHolderAnimation")));

            // --- Chase ---
            SequenceNode chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(_player, transform, _agent, () => EffectiveChaseSpeed, _enemyStats.AttackRange)));

            // --- Patrol ---
            LeafNode patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, _enemyStats.PatrolSpeed), 0);

            root.AddChild(fleeAndAttackSequence);
            root.AddChild(attackSequence);
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);
            return tree;
        }
        // Replace for a list of posible attacks
        public void FireProjectile()
        {
            Vector3 dir = (_player.position - transform.position).normalized;
            float spawnOffset = 1.0f;
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f + dir * spawnOffset;
            var go = Helpers.ProjFactory.Spawn(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = go.GetComponent<EnemyProjectile>();
            proj.Init(dir, _projectileSpeed, (int)EffectiveAttackDamage, Foundation.ElementType.Neutral);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _enemyStats.DangerRange);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _enemyStats.ExitDangerRange);
        }
    }
}
