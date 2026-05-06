using System.Collections.Generic;
using UnityEngine;

public static class CoroutineUtils
{
    private static Dictionary<float, WaitForSeconds> _lookUp = new();

    public static WaitForSeconds GetWait(float wait)
    {
        if (!_lookUp.ContainsKey(wait))
            _lookUp[wait] = new WaitForSeconds(wait);

        return _lookUp[wait];
    }
}