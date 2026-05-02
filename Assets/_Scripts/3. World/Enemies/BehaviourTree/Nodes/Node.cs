using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace World 
{ 

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
    public class BehaviourTree : Node
    {
        public BehaviourTree(string name) : base(name) { }

        public override NodeState Process()
        {

            if (_children.Count == 0)
                return NodeState.Failure;

            return _children[0].Process();

            /*
            while (_currentChild < _children.Count) 
            {
                var status = _children[_currentChild].Process();
                if (status != NodeState.Success) 
                {
                    return status;
                }
                _currentChild++;
            }
            //Reset(); //solo test
            Debug.Log("Termino???");
            return NodeState.Success;*/
        }
        /*
        public override NodeState Process()
        {
            NodeState status = _children[_currentChild].Process();

            _currentChild = (_currentChild + 1) % _children.Count;
            return NodeState.Running;
        }*/
    }
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
    public class ConditionNode : IStrategy
    {
        readonly Func<bool> predicate;
        public ConditionNode(Func<bool> predicate)
        {
            this.predicate = predicate;
        }
        public Node.NodeState Process() => predicate() ? Node.NodeState.Success : Node.NodeState.Failure;

        //public void Reset() { } anulada por la interfaz
    }
    public class ActionNode : IStrategy
    {
        readonly Action doSomething;

        public ActionNode(Action doSOmething)
        {
            this.doSomething = doSOmething;
        }

        public Node.NodeState Process()
        {
            doSomething();
            return Node.NodeState.Success;
        }
    }
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
    public class RepeatNode : Node
    {
        readonly int times;
        int currentCount;

        public RepeatNode(string name, int times) : base(name)
        {
            this.times = times;
        }

        public override NodeState Process()
        {
            if (_children.Count == 0)
                return NodeState.Failure;

            var child = _children[0];

            var result = child.Process();

            switch (result)
            {
                case NodeState.Running:
                    return NodeState.Running;

                case NodeState.Failure:
                    Reset();
                    return NodeState.Failure;

                case NodeState.Success:
                    currentCount++;
                    child.Reset();

                    if (currentCount >= times)
                    {
                        Reset();
                        return NodeState.Success;
                    }
                    return NodeState.Running;
            }
            return NodeState.Failure;
        }
        public override void Reset()
        {
            base.Reset();
            currentCount = 0;
        }
    }
    public class PrioritySelectorNode : Node
    {
        List<Node> sortedChildren;
        List<Node> SortedChildren => sortedChildren ??= SortChildren();

        protected virtual List<Node> SortChildren() => _children.OrderByDescending(child => child._priority).ToList();

        public PrioritySelectorNode(string name) : base(name) { }

        public override void Reset()
        {
            base.Reset();
            sortedChildren = null;
        }

        public override NodeState Process()
        {
            foreach (var child in SortedChildren)
            {
                switch (child.Process())
                {
                    case NodeState.Running:
                        return NodeState.Running;
                    case NodeState.Success:
                        return NodeState.Success;
                    default:
                        continue;
                }
            }
            return NodeState.Failure;
        }
    }
    public class RandomSelectorNode : PrioritySelectorNode
    {
        protected override List<Node> SortChildren() => _children.Shuffle();

        public RandomSelectorNode(string name) : base(name) { }
    }
    public static class ListExtensions
    {
        //For random SelectorNode
        public static List<T> Shuffle<T>(this List<T> list)
        {
            var rng = new System.Random();
            var shuffled = new List<T>(list);

            int n = shuffled.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (shuffled[k], shuffled[n]) = (shuffled[n], shuffled[k]);
            }

            return shuffled;
        }
    }
    public class InverterNode : Node
    {
        public InverterNode(string name) : base(name) { }

        public override NodeState Process()
        {
            switch (_children[0].Process())
            {
                case NodeState.Running:
                    return NodeState.Running;
                case NodeState.Failure:
                    return NodeState.Success;
                default:
                    return NodeState.Failure;
            }
        }
    }
    public class UntilFailNode : Node
    {
        //Until Success
        public UntilFailNode(string name) : base(name) { }

        public override NodeState Process()
        {
            if (_children[0].Process() == NodeState.Failure)
            {
                Reset();
                return NodeState.Failure;
            }
            return NodeState.Running;
        }
    }
}

