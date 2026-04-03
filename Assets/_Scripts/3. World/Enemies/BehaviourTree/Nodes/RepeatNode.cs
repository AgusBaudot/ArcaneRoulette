using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace world 
{
    public class RepeatNode : Node
    {
        readonly int times;
        public RepeatNode(string name, int times) : base(name) 
        {
            this.times = times;
        }

        public override NodeState Process()
        {
            for (int i = 0; i < times; i++) 
            {
                _children[0].Process();
            }
            return NodeState.Success; //NOT FINISHED
        }
    }
}

