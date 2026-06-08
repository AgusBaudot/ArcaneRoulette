using UnityEngine;

namespace World 
{
    public class AIBruto : AIBrain
    {
        [Header("Melee Internal values")]
        [SerializeField] private ExplosionArea _explosionAreaPrefab;
        [SerializeField] private float _radius = 4f;
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
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, () => _currentAttackSpeed, "BrutoPHAnim")));
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
        public void DoAreaAttack()
        {
            Vector3 dir = (target.position - transform.position).normalized;
            Vector3 pos = transform.position + dir * 3f;
            var explosion = Instantiate(_explosionAreaPrefab, pos, Quaternion.identity);
            explosion.Init(_radius, _currentAttackDamage, _playerLayer, target, 1);
        }
    }


}
