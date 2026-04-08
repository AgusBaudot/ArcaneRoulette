using System.Collections.Generic;
using UnityEngine;
using World;
using static UnityEditor.Rendering.FilterWindow;
using static UnityEngine.GraphicsBuffer;

namespace world 
{
    public class AIMelee : AIBrain // Hereda Monobehaviour
    {
        [Header("Melee Settings")]
        [SerializeField] private float attackRange;
        [SerializeField] private float chaseSpeed;
        [SerializeField] private float patrolSpeed;

        [SerializeField] private List<Transform> waypoints;
        [SerializeField] private float attackCooldown;
        protected override void Awake()
        {
            base.Awake();
        }
        protected override void Update()
        {
            base.Update();
        }

        protected override BehaviourTree BuildTree() 
        {
            var tree = new BehaviourTree(base._behaviourTreeName);
            var root = new PrioritySelectorNode("Root");

            // --- Attack Sequence ---
            var attackSequence = new SequenceNode("Attack",2);
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => Vector3.Distance(transform.position, target.position) <= attackRange)));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(attackCooldown, _animator)));

            // --- Chase ---
            var chaseSequence = new SequenceNode("Chase",1);
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(IsInLos)));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, _agent, chaseSpeed)));

            // --- Patrol ---
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, waypoints, patrolSpeed), 0);

            // --- Estructura ---
            //root.AddChild(attackSequence);
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }
        public void PrintLog() 
        {
            Debug.Log("Disparo");
        }
        /*
        public void FireProjectile(Transform target, Transform firePoint)
        {
            Vector3 dir = (target.position - firePoint.position).normalized;

            var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            var proj = go.GetComponent<EnemyProjectile>();
            proj.Init(dir, speed, damage, element);
        }*/
    }
}

