using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public sealed class AIMelee : AIBrain
    {
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
            chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, transform ,_agent, _chaseSpeed)));
            chaseSequence.AddChild(new LeafNode("wait", new Wait(_attackSpeed)));

            // --- Patrol ---
            var patrol = new LeafNode("Patrol", new Patrol(transform, _agent, _waypoints, _patrolSpeed), 0);

            // --- Estructura ---
            root.AddChild(chaseSequence);
            root.AddChild(patrol);

            tree.AddChild(root);

            return tree;
        }
    }
}

