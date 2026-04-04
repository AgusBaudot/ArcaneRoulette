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
        [SerializeField] private Animation attackAnimation;
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

            return tree;
        }
    }
}

