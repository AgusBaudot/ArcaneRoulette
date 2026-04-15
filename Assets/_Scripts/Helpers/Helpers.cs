using System.Collections.Generic;
using UnityEngine;
//using DG.Tweening;

public static class Helpers
{
    private static Camera MainCamera;
    
    public static Camera GetCamera()
    {
        MainCamera = Camera.main;
        if (MainCamera == null)
            Debug.LogError("CRITICAL: Camera.main is null! The scene is missing one.");
        
        return MainCamera;
    }

    private static Dictionary<float, WaitForSeconds> lookUp = new();

    public static WaitForSeconds GetWait(float wait)
    {
        if (!lookUp.ContainsKey(wait))
            lookUp[wait] = new WaitForSeconds(wait);

        return lookUp[wait];
    }
    
    /*
     AFTER IMPORTING DOTWEEN
     
     
     public static void FadeIn(CanvasGroup canvasGroup, float fadeTime, float fadeTo = 1f)
     {
        canvasGroup.DOFade(fadeTo, fadeTime);
     }
     
     public static void FadeOut(CanvasGroup canvasGroup, float fadeTime, float fadeTo = 0f)
     {
        canvasGroup.DOFade(fadeTo, fadeTime);
     }
     */
}