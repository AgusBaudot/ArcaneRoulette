using System.Collections;
using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;
using World;

public class AIBruto : AIBrain
{

    [Header("Bruto Settings")]
    [SerializeField] private float exitAttackRange; // it must always be greater than _attackRange
    [SerializeField] private float attackRadius;
    private bool _wasInRange;

    [Header("Prefab")]
    [SerializeField] private ExplosionArea _explosionAreaPrefab;

    [Header("Explosion")]
    [SerializeField] private float _radius = 4f;
    [SerializeField] private LayerMask _playerLayer;
    protected override void Awake()
    {
        base.Awake();
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
    } // Change the method for GetIdealRange to make it more accurate and expose the result into the blackboard
    protected override BehaviourTree BuildTree()
    {
        var tree = new BehaviourTree(base._behaviourTreeName);
        var root = new PrioritySelectorNode("Root");

        // --- Attack Sequence ---
        var attackSequence = new SequenceNode("Attack", 2);
        attackSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInAttackRangeStable())));
        attackSequence.AddChild(new LeafNode("Attack", new Attack(_animator, _attackSpeed, "BrutoPHAnim")));
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
    public void DoAreaAttack()
    {
        Vector3 dir = (target.position - transform.position).normalized;
        Vector3 pos = transform.position + dir * 1.5f;
        var explosion = Instantiate(_explosionAreaPrefab, pos, Quaternion.identity);
        explosion.Init(_radius, _attackDamage, _playerLayer, target, 1);
    }
}

