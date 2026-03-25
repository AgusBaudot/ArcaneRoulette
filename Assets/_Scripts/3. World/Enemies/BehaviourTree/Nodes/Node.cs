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
            
        public readonly List<Node> _children = new();
        protected int _currentChild;
    
        public Node(string name = "Node") 
        {
            this._name = name;
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

