using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class AIRange : AIBrain
    {
        [Header("Range Settings")]
        [SerializeField] private float exitAttackRange; // it must always be greater than _attackRange
        [SerializeField] private GameObject projectilePrefab;
        private bool _wasInRange;
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
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInAttackRangeStable())));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _attackSpeed, "PlaceHolderAnimation")));
            //attackSequence.AddChild(new LeafNode("wait", new Wait(_cooldown)));

            // --- Chase ---
            var chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, transform, _agent, _chaseSpeed)));

            // --- Patrol ---
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, _patrolSpeed), 0);

            // --- Estructura ---
            root.AddChild(attackSequence);
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }
        bool IsInAttackRangeStable()
        {
            float distance = Vector3.Distance(transform.position, target.position);
            bool result;
            if (_wasInRange)
                result = distance <= exitAttackRange;
            else
                result = distance <= _attackRange;
            _wasInRange = result;
            return result;
        }// Change the method for GetIdealRange to make it more accurate

        // Reemplazar por una lista de ataques posibles
        public void FireProjectile()
        {
            Vector3 dir = (target.position - transform.position).normalized;

            float spawnOffset = 1.0f;

            Vector3 spawnPos = transform.position + dir * spawnOffset;

            var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = go.GetComponent<EnemyProjectile>();
            proj.Init(dir, 10, 2, Foundation.ElementType.Neutral);
        }
    }

}
