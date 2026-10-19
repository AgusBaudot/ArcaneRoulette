using System;
using UnityEngine;

namespace Meta
{
    [Serializable]
    public class SettingsSaveData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.2f;
        public float SFXVolume = 1f;
        public float UIVolume = 1f;
        public float AmbienceVolume = 1f;

        public int ResolutionWidth;
        public int ResolutionHeight;
        public int RefreshRate;
        public FullScreenMode WindowMode;

        public SettingsSaveData()
        {
#if UNITY_EDITOR
            // Safe fallbacks to prevent log spam during editor playtesting
            ResolutionWidth = 1920;
            ResolutionHeight = 1080;
            RefreshRate = 60;
            WindowMode = FullScreenMode.Windowed;
#else
            // Real hardware query compiled exclusively into the build
            Resolution currentRes = Screen.currentResolution;
            
            ResolutionWidth = currentRes.width;
            ResolutionHeight = currentRes.height;
            RefreshRate = Mathf.RoundToInt((float)currentRes.refreshRateRatio.value);
            
            WindowMode = FullScreenMode.FullScreenWindow;
#endif
        }

        public static SettingsSaveData GetDefault() => new SettingsSaveData();
    }
}