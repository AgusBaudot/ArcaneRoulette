using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using world;

public class TaskNode : Node , ITreeNode
{
    private Action task;

    public TaskNode(Action task)
    {
        this.task = task;
    }

    public void Execute()
    {
        task?.Invoke();
    }

    public override NodeState Tick()
    {
        return NodeState.Success; //Just For EXample
    }
}
