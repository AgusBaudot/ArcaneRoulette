using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace World 
{
    public class PrioritySelectorNode : Node
    {
        List<Node> sortedChildren;
        List<Node> SortedChildren => sortedChildren ??= SortChildren();

        protected virtual List<Node> SortChildren() => _children.OrderByDescending(child => child._priority).ToList();

        public PrioritySelectorNode (string name) : base (name) { }

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
}

