using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace world 
{
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
}


    
