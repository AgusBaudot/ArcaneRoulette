using Core;
using UnityEngine;

public static class Helpers
{
    private static CombatSettings _combatSettings;
    private static InputReader _input;

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

    public static InputReader Input
    {
        get
        {
            if (!_input)
            {
                _input = Resources.Load<InputReader>("InputReader");
                
                if (!_input)
                    Debug.LogError("CRITICAL: Could not find InputReader in Resources folder!");
            }

            return _input;
        }
    }
}