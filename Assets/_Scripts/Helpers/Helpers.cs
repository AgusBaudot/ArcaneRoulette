using Core;
using UnityEngine;

public static class Helpers
{
    private static CombatSettings _combatSettings;
    private static InputReader _input;
    private static PlayerStats _playerStats;

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
            if (_input == null)
            {
                _input = Resources.Load<InputReader>("InputReader");
                
                if (_input == null)
                    Debug.LogError("CRITICAL: Could not find InputReader in Resources folder!");
            }

            return _input;
        }
    }

    public static PlayerStats PlayerStats
    {
        get
        {
            if (_playerStats == null)
            {
                _playerStats = Resources.Load<PlayerStats>("PlayerStats");
                
                if (_input == null)
                    Debug.LogError("CRITICAL: Could not find PlayerStats in Resources folder!");
            }
            
            return _playerStats;
        }
    }
}