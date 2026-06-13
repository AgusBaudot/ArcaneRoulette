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
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInStableDistance(_player))));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _agent ,() => EffectiveAttackSpeed, "PlaceHolderAnimation")));

            // --- Chase ---
            SequenceNode chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(_player, transform, _agent, () => EffectiveChaseSpeed)));

            // --- Patrol ---
            LeafNode patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, _enemyStats.PatrolSpeed), 0);

            // --- Estructura ---
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
    }
}
