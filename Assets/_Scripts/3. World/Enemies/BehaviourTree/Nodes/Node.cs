using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Node
{
    public abstract NodeState Tick();
}

public enum NodeState
{
    Success,
    Failure,
   // Running
}
