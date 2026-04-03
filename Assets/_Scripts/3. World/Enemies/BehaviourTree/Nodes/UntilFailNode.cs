using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace world 
{
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

