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

        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public int RefreshRate = 60;
        public FullScreenMode WindowMode = FullScreenMode.FullScreenWindow;

        public static SettingsSaveData GetDefault() => new SettingsSaveData();
    }
}