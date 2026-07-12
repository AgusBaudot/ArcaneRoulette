using System;
using System.IO;
using UnityEngine;

namespace Meta
{
    public class SaveLoadSystem : ISettingsSave
    {
        private string SettingsFilePath => Path.Combine(Application.persistentDataPath, "settings_vault.json");
        
        public SettingsSaveData LoadSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new SettingsSaveData();
            }

            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonUtility.FromJson<SettingsSaveData>(json) ?? new SettingsSaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadSystem] Failed to read settings: {e.Message}");
                return new SettingsSaveData();
            }
        }

        public void SaveSettings(SettingsSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true); //true = pretty print for readability
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadSystem] Failed to write settings: {e.Message}");
            }
        }
    }
}