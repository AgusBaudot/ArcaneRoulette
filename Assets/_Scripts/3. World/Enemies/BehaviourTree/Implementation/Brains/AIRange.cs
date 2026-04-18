using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace World 
{
    public class AIRange : AIBrain
    {
        [Header("Range Settings")]
        [SerializeField] private float attackRange;
        [SerializeField] private float exitAttackRange;
        [SerializeField] private float _cooldown;
        private bool _wasInRange;
        [SerializeField] private float chaseSpeed;
        [SerializeField] private float patrolSpeed;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private List<Transform> waypoints;
        BlackboardKey hasSeenPlayerKey;
        protected override void Awake()
        {
            base.Awake();
            waypoints.Clear();
            waypoints.Add(target);
        }
        protected override void Update()
        {
            base.Update();
            if (IsInLos())
                blackboard.SetValue(hasSeenPlayerKey, true);
        }

        bool IsInAttackRangeStable()
        {
            float distance = Vector3.Distance(transform.position, target.position);
            bool result;
            if (_wasInRange)
                result = distance <= exitAttackRange;
            else
                result = distance <= attackRange;
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
            attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _cooldown)));
            //attackSequence.AddChild(new LeafNode("wait", new Wait(_cooldown)));

            // --- Chase ---
            var chaseSequence = new SequenceNode("Chase", 1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => { return blackboard.TryGetValue<bool>(hasSeenPlayerKey, out var seen) && seen; })));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, transform, _agent, chaseSpeed)));

            // --- Patrol ---
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, waypoints, patrolSpeed), 0);

            // --- Estructura ---
            root.AddChild(attackSequence);
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }

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
