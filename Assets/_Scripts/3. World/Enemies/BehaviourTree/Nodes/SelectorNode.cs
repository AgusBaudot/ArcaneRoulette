using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    public class SelectorNode : Node
    {
        public SelectorNode(string name, int priority = 0) : base(name, priority) { }

        public override NodeState Process()
        {
            Debug.Log("Hijo Actual=" + _children[_currentChild]._name);
            if (_currentChild < _children.Count)
            {
                switch (_children[_currentChild].Process())
                {
                    case NodeState.Running:
                        //_currentChild = 0; // este nodo deberia ser como el de prioridad
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

