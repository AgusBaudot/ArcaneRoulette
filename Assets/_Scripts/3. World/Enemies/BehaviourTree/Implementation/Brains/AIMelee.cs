using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class AIMelee : AIBrain
    {
        [Header("Melee Settings")]
        [SerializeField] private float attackRange;
        [SerializeField] private float chaseSpeed;
        [SerializeField] private float patrolSpeed;
        [SerializeField] private float _cooldown;

        protected override void Awake()
        {
            base.Awake();
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
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, patrolSpeed), 0);

            // --- Estructura ---
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }
    }
}

