using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using world;

public class LeafNode : Node
{
    readonly IStrategy strategy;
    public LeafNode(string name, IStrategy strategy, int priority = 0) : base(name, priority)
    {
        this.strategy = strategy;
    }
    public override NodeState Process() => strategy.Process();
    public override void Reset() => strategy.Reset();
}
