using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


namespace world 
{
    public interface IStrategy
    {
        Node.NodeState Process();
        void Reset() 
        {
            //Null
        }
    }

}
