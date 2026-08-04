using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation;
using Core;
using World;

public class Cheats : MonoBehaviour
{
    public static Cheats Instance { get; private set; }
    public static bool GodMode { get; private set; }

    private Dictionary<string, Func<string[], string>> _commands = new();
    private Dictionary<string, RuneDefinitionSO> _runeDatabase;

    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;

        _runeDatabase = Resources.LoadAll<RuneDefinitionSO>("Runes")
            .ToDictionary(r => r.name.ToLower().Replace(" ", ""), r => r);

        RegisterCommands();
    }

    private void RegisterCommands()
    {
        _commands.Add("help", args =>
        {
            return "<color=#3C3C3C>Available commands:\n" +
                   "  god - Toggles invincibility\n" +
                   "  heal [amount] - Heals player\n" +
                   "  clearroom - Forces room clear event\n" +
                   "  nextfloor - Advances 1 floor\n" +
                   "  floor [number] - Advances to specific floor\n" +
                   "  tp portal - Teleports to portal room\n" +
                   "  give [currency|rune] [id] [amount] - Gives items\n" +
                   "  help - Shows this list</color>";
        });

        _commands.Add("god", args => 
        {
            GodMode = !GodMode;
            return $"God mode set to: {(GodMode ? "<color=#00FF00>ON</color>" : "<color=#FF0000>OFF</color>")}";
        });

        _commands.Add("heal", args =>
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int amount)) return "<color=#FF0000>Usage: heal [amount]</color>";
            var player = FindObjectOfType<PlayerController>();
            if (player != null && player.GetComponentInChildren<IHealable>() is IHealable healable)
            {
                healable.Heal(amount);
                return $"Healed player for {amount} HP.";
            }
            return "<color=#FF0000>Player or IHealable not found.</color>";
        });

        _commands.Add("clearroom", args =>
        {
            if (GameStateManager.RunState == null) return "<color=#FF0000>Not in an active run.</color>";
            EventBus.Publish(new RoomClearEvent(GameStateManager.RunState.CurrentRoomIndex));
            return "Forced room clear event.";
        });

        _commands.Add("nextfloor", args => AdvanceFloor(1));

        _commands.Add("floor", args =>
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int targetFloor)) return "<color=#FF0000>Usage: floor [number]</color>";
            return AdvanceFloor(targetFloor - GameStateManager.RunState.CurrentFloor);
        });

        _commands.Add("tp", args =>
        {
            if (args.Length < 2 || args[1] != "portal") return "<color=#FF0000>Usage: tp portal</color>";
            return TeleportToPortal();
        });

        _commands.Add("give", args =>
        {
            if (args.Length < 3) return "<color=#FF0000>Usage: give [currency|rune] [amount/id]</color>";
            return ProcessGiveCommand(args);
        });
    }

    public string ExecuteCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        
        string[] split = input.ToLower().Split(' ');
        string command = split[0];
        string echo = $"<color=#FFFFFF>> {input}</color>";

        if (_commands.TryGetValue(command, out var action))
        {
            string result = action.Invoke(split);
            return $"{echo}\n{result}";
        }
        
        return $"{echo}\n<color=#FF0000>Unknown command '{command}'. Type 'help' for a list of commands.</color>";
    }

    private string AdvanceFloor(int floorsToAdvance)
    {
        if (GameStateManager.RunState == null) return "<color=#FF0000>Not in an active run.</color>";
        GameStateManager.RunState.CurrentFloor += floorsToAdvance;
        EventBus.Publish(new FloorTransitionRequestEvent(SceneNames.GameLevel));
        return $"Advancing to floor {GameStateManager.RunState.CurrentFloor}...";
    }

    private string TeleportToPortal()
    {
        var runState = GameStateManager.RunState;
        if (runState == null) return "<color=#FF0000>Not in an active run.</color>";

        foreach (var kvp in runState.FloorMap)
        {
            if (kvp.Value.Type == RoomType.Portal)
            {
                var rooms = FindObjectsOfType<RoomManager>();
                var portalRoom = rooms.FirstOrDefault(r => r.Index == kvp.Key);
                if (portalRoom != null)
                {
                    EventBus.Publish(new PlayerTeleportRequestEvent(portalRoom.transform.position));
                    runState.UpdatePlayerRoom(kvp.Key);
                    return "Teleported to Portal Room.";
                }
            }
        }
        return "<color=#FF0000>No portal room found on this floor (is it a boss floor?).</color>";
    }

    private string ProcessGiveCommand(string[] args)
    {
        if (GameStateManager.RunState == null) return "<color=#FF0000>Not in an active run.</color>";

        if (args[1] == "currency")
        {
            if (int.TryParse(args[2], out int amount))
            {
                GameStateManager.RunState.AddCurrency(amount);
                return $"Added {amount} currency.";
            }
        }
        else if (args[1] == "rune")
        {
            if (args.Length < 4 || !int.TryParse(args[3], out int amount)) return "<color=#FF0000>Usage: give rune [id] [amount]</color>";
            string runeId = args[2];
            
            if (_runeDatabase.TryGetValue(runeId, out var runeSO))
            {
                GameStateManager.RunState.AddRune(runeSO, amount);
                return $"Gave {amount}x {runeSO.Name}.";
            }
            return $"<color=#FF0000>Rune '{runeId}' not found in Resources.</color>";
        }
        return "<color=#FF0000>Invalid give parameter. Use 'currency' or 'rune'.</color>";
    }
}