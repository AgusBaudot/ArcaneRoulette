using System.Collections.Generic;
using Core;
using UnityEngine;
//using DG.Tweening;

public static class Helpers
{
    private static Camera MainCamera;
    private static CombatSettings _combatSettings;
    
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

    public static CombatSettings Combat
    {
        get
        {
            if (_combatSettings == null)
            {
                _combatSettings = Resources.Load<CombatSettings>("CombatSettings");
                if (_combatSettings == null)
                    Debug.LogError("CRITICAL: Could not find CombatSettings in Resources folder!");
            }

            return _combatSettings;
        }
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