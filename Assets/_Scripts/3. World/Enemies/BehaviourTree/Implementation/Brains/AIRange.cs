using UnityEngine;

namespace World 
{
    public class AIRange : AIBrain
    {
        [Header("Range Prefab")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private float _projectileSpeed;

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
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, () => _currentAttackSpeed, "PlaceHolderAnimation")));
            //attackSequence.AddChild(new LeafNode("wait", new Wait(_cooldown)));

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

        // Reemplazar por una lista de ataques posibles
        public void FireProjectile()
        {
            Vector3 dir = (target.position - transform.position).normalized;

            float spawnOffset = 1.0f;

            Vector3 spawnPos = transform.position + Vector3.up * 0.5f + dir * spawnOffset;


            var go = Helpers.ProjFactory.Spawn(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = go.GetComponent<EnemyProjectile>();
            proj.Init(dir, _projectileSpeed, (int)_currentAttackDamage, Foundation.ElementType.Neutral);
        }
    }

}
