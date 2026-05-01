using Core;
using UnityEngine;

public static class GameContext
{
    public static InputReader Input { get; private set; }
    public static CombatSettings Combat { get; private set; }

    /// <summary>
    /// Called once by a GameBootstrapper scripts when the game starts
    /// </summary>
    public static void Initialize()
    {
        Input = Resources.Load<InputReader>("InputReader");
        Combat = Resources.Load<CombatSettings>("CombatSettings");
    }
}