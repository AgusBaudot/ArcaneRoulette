using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using world;
using World;

public class AIRange : AIBrain
{
    [Header("Range Settings")]
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
        var attackSequence = new SequenceNode("Attack", 2);
        attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInRange(attackRange))));
        attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator)));
        attackSequence.AddChild(new LeafNode("wait", new Wait(_cooldown)));

        // --- Chase ---
        var chaseSequence = new SequenceNode("Chase", 1);
        chaseSequence.AddChild(new LeafNode("HasLOS", new ConditionNode(() => IsInLos())));
        chaseSequence.AddChild(new LeafNode("Chase", new Chase(target, _agent, chaseSpeed)));

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
        proj.Init(dir, 10, 20, Foundation.ElementType.Neutral);
    }
}
