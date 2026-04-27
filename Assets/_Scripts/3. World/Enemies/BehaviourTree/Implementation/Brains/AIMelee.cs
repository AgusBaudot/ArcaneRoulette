using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class AIMelee : AIBrain // Hereda Monobehaviour
    {
        [Header("Melee Settings")]
        [SerializeField] private float attackRange;
        [SerializeField] private float chaseSpeed;
        [SerializeField] private float patrolSpeed;

        [SerializeField] private List<Transform> waypoints;
        [SerializeField] private float _cooldown;
        [SerializeField] private GameObject projectilePrefab;
        //[SerializeField] private Transform firePoint;
        protected override void Awake()
        {
            base.Awake();
            waypoints.Add(target);
        }
        protected override void Update()
        {
            base.Update();
        }

        protected override BehaviourTree BuildTree() 
        {
            var tree = new BehaviourTree(base._behaviourTreeName);
            var root = new PrioritySelectorNode("Root");

            // --- Chase ---
            var chaseSequence = new SequenceNode("Chase",1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, transform ,_agent, chaseSpeed)));
            chaseSequence.AddChild(new LeafNode("wait", new Wait(_cooldown)));

            // --- Patrol ---
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, waypoints, patrolSpeed), 0);

            // --- Estructura ---
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }
        public void PrintLog() 
        {
            Debug.Log("DISPARO");
        }

        public void FireProjectile()
        {
            Vector3 dir = (target.position - transform.position).normalized;

            float spawnOffset = 1.0f;

            Vector3 spawnPos = transform.position + dir * spawnOffset;

            var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = go.GetComponent<EnemyProjectile>();
            proj.Init(dir, 10, 20, Foundation.ElementType.Neutral);
        }
    }
}

