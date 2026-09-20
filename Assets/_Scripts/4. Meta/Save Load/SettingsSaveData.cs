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
            Resolution currentRes = Screen.currentResolution;
            
            ResolutionWidth = currentRes.width;
            ResolutionHeight = currentRes.height;
            RefreshRate = Mathf.RoundToInt((float)currentRes.refreshRateRatio.value);
            
            WindowMode = FullScreenMode.FullScreenWindow;
        }

        public static SettingsSaveData GetDefault() => new SettingsSaveData();
    }
}