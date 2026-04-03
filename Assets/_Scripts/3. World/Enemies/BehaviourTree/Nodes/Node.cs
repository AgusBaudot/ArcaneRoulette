using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace world { 

    public abstract class Node
    {  
        public enum NodeState
        {
            Success,
            Failure,
            Running
        }

        public readonly string _name;
        public readonly int _priority;
            
        public readonly List<Node> _children = new();
        protected int _currentChild;

        public Node(string name = "Node", int priority = 0) 
        {
            this._name = name;
            this._priority = priority;
        }

        public void AddChild(Node child) => _children.Add(child);
            
        public virtual NodeState Process() => _children[_currentChild].Process();
        
        public virtual void Reset() 
        {
            _currentChild = 0;
            foreach (Node child in _children) 
            {
                child.Reset();
            }
        }
    }
}

