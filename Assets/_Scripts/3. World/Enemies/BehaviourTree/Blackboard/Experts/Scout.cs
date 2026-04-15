using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World;

public class Scout : MonoBehaviour, IExpert
{
   public int GetInsistence(Blackboard blackboard) 
    {
        //return "value" ? 100 : 0;  
        return 0;
    }

    public void Execute (Blackboard blackboard) 
    { /*
        blackboard.AddAction(() =>
        {
            if (blackboard.TryGetValue(isSafeKey, out bool isSafe))
            {
                blackboard.SetValue(isSafeKey, !isSafe);
            }
        });
        */
    }
}
