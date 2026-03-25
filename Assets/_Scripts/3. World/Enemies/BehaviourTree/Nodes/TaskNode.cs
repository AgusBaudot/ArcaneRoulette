using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using world;

public class TaskNode : Node
{
    readonly IStrategy strategy;
    public TaskNode(string name, IStrategy strategy) : base(name)
    {
        this.strategy = strategy;
    }
    public override NodeState Process() => strategy.Process();
    public override void Reset() => strategy.Reset();
}
