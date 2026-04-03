using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace world 
{
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
}

