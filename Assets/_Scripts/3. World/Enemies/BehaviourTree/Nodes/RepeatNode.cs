using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
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
}

