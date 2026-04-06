using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using world;

public class ConditionNode : IStrategy
{
    readonly Func<bool> predicate;

    public ConditionNode (Func<bool> predicate) 
    {
        this.predicate = predicate;
    }

    public Node.NodeState Process() => predicate () ? Node.NodeState.Success : Node.NodeState.Failure;

    //public void Reset() { } anulada por la interfaz
}
