using Core;
using Foundation;
using UI;
using UnityEngine;

public static class Helpers
{
    private static CombatSettings _combatSettings;
    private static InputReader _input;
    private static PlayerStats _playerStats;
    private static ProjectilePrefabFactory _projectilePrefabFactory;
    private static PlayerAudioProfileSO _playerAudioProfile;
    private static UIAudioProfileSO _uiAudioProfile;

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

    public static ProjectilePrefabFactory ProjFactory
    {
        get
        {
            if (_projectilePrefabFactory == null)
            {
                _projectilePrefabFactory = ProjectilePrefabFactory.Instance;
                
                if (_projectilePrefabFactory == null)
                    Debug.LogError($"CRITICAL: Instance of {nameof(ProjectilePrefabFactory)} is null!");
            }
            
            return _projectilePrefabFactory;
        }
    }

    public static PlayerAudioProfileSO PlayerAudio
    {
        get
        {
            if (_playerAudioProfile == null)
            {
                _playerAudioProfile = Resources.Load<PlayerAudioProfileSO>("PlayerProfile");
                
                if (_playerAudioProfile == null)
                    Debug.LogError($"CRITICAL: Could not find Player Audio Profile in Resources folder!");
            }
            
            return _playerAudioProfile;
        }
    }

    public static UIAudioProfileSO UIAudio
    {
        get
        {
            if (_uiAudioProfile == null)
            {
                _uiAudioProfile = Resources.Load<UIAudioProfileSO>("UIProfile");
                
                if (_uiAudioProfile == null)
                    Debug.LogError("CRITICAL: Could not find UI Audio Profile in Resources folder!");
            }
            
            return _uiAudioProfile;
        }
    }
}