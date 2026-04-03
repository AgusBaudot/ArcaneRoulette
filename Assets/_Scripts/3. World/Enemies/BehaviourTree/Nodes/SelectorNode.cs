using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace world 
{
    public class SelectorNode : Node
    {
        public SelectorNode(string name, int priority = 0) : base(name, priority) { }

        public override NodeState Process()
        {
            if (_currentChild < _children.Count)
            {
                switch (_children[_currentChild].Process())
                {
                    case NodeState.Running:
                        return NodeState.Running;
                    case NodeState.Success:
                        Reset();
                        return NodeState.Success;
                    default:
                        _currentChild++;
                        return NodeState.Running;
                }
            }

            Reset();
            return NodeState.Failure;
        }
    }
}

