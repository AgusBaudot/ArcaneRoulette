using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using world;

public class SequenceNode : Node
{
    public SequenceNode(string name, int priority = 0) : base(name, priority) { }

    public override NodeState Process()
    {
        if (_currentChild < _children.Count)
        {
            switch (_children[_currentChild].Process())
            {
                case NodeState.Running:
                    _currentChild = 0; // Esto se tiene que chequear
                    return NodeState.Running;
                case NodeState.Failure:
                     Reset();
                    return NodeState.Failure;
                default:
                    _currentChild++;
                    return _currentChild == _children.Count ? NodeState.Success : NodeState.Running; // if it is last node the Sequence return success
            }
        }

        Reset();
        return NodeState.Success;
    }
}
