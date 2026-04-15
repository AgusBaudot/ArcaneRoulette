using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;
using World;

public class AIBruto : AIBrain
{

    [Header("Range Settings")]
    [SerializeField] private float attackRange;
    [SerializeField] private float exitAttackRange;
    [SerializeField] private float attackRadius;
    [SerializeField] private int _damage;
    [SerializeField] private ElementType _element = ElementType.Neutral;
    [SerializeField] private float _cooldown;
    private bool _wasInRange;
    [SerializeField] private float chaseSpeed;
    [SerializeField] private float patrolSpeed;
    [SerializeField] private GameObject FVXPrefab;
    [SerializeField] private float vfxDuration = 1f;
    [SerializeField] private LayerMask damageMask;
    [SerializeField] private List<Transform> waypoints;
    BlackboardKey hasSeenPlayerKey;
    protected override void Awake()
    {
        base.Awake();
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



    void PlayAttackVFX()
    {
        Vector3 dir = (target.position - transform.position).normalized;
        Vector3 pos = transform.position + dir * 2f;
        var vfx = Instantiate(FVXPrefab, pos, Quaternion.LookRotation(dir));
        Destroy(vfx, vfxDuration);
    }
    public void DoAreaAttack()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRadius, damageMask);
        PlayAttackVFX();
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(_damage, _element);
            }
        }
    }


}

