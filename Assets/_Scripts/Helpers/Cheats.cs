using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Foundation
{
    public class Cheats : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Input;

        private readonly string _scene1 = "Core Loop";
        private readonly string _scene2 = "Hardcore Room";

        private static readonly Dictionary<string, CommandEntry> _commands = new();

        public struct CommandEntry
        {
            public Action<string[]> Handler;
            public string Description;
        }

        #region Unity Lifecycle
        private void OnEnable() => UpdateManager.Instance.Register(this);

        private void OnDisable() => UpdateManager.Instance?.Unregister(this);
        #endregion

        public void Tick(float dt)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (SceneManager.GetActiveScene().name == _scene1)
                    return;
                SceneManager.LoadScene(_scene1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (SceneManager.GetActiveScene().name == _scene2)
                    return;
                SceneManager.LoadScene(_scene2);
            }
        }

        public static void RegisterCommand(string id, Action<string[]> handler, string description)
        {
            if (!_commands.TryAdd(id.ToLower(), new CommandEntry { Handler = handler, Description = description }))
            {
                Debug.LogWarning($"[Cheats] Command '{id}' is already registered.");
            }
        }

        public static void UnregisterCommand(string id)
        {
            _commands.Remove(id.ToLower());
        }

        public static void ExecuteCommand(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
                return;

            string[] parts = rawInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandId = parts[0].ToLower();
            string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            if (_commands.TryGetValue(commandId, out var entry))
            {
                entry.Handler?.Invoke(args);
            }
            else
            {
                Debug.LogWarning($"[Cheats] Unknown command: {commandId}");
            }
        }
    }

}