using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine;

namespace world 
{
    public class AIMelee : AIBrain // Hereda Monobehaviour
    {
        [Header("Melee Settings")]
        [SerializeField] private float attackRange;
        [SerializeField] private float chaseSpeed;
        [SerializeField] private float patrolSpeed;

        [SerializeField] private List<Transform> waypoints;
        [SerializeField] private Animator attackAnimation;
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
            var root = new SelectorNode("Root");
            
            // --- Attack Sequence ---
            var attackSequence = new SequenceNode("Attack");
            attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => Vector3.Distance(transform.position, target.position) <= attackRange)));
            attackSequence.AddChild(new LeafNode("Attack", new Attack(attackCooldown, attackAnimation)));

            // --- Chase ---
            var chaseSequence = new SequenceNode("Chase");
            chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, _agent, chaseSpeed)));

            // --- Patrol ---
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, waypoints, patrolSpeed));

            // --- Estructura ---
            //root.AddChild(attackSequence);
            //root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }
    }
}

