using System.Collections.Generic;
using Foundation;
using Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SettingsUI : BaseUIPanel
    {
        [Header("Audio Controls")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        
        [Header("Audio Labels")]
        [SerializeField] private TMP_Text _masterValueText;
        [SerializeField] private TMP_Text _musicValueText;
        [SerializeField] private TMP_Text _sfxValueText;
        
        [Header("Display Controls")]
        [SerializeField] private TMP_Dropdown _windowModeDropdown;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        
        [Header("Actions")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _backButton;

        private SettingsSaveData _workingCache;
        private ISettingsSave _saveLoadSystem;
        private Resolution[] _availableResolutions;

        private readonly string[] _windowModeNames =
        {
            "FullScreen",
            "Borderless Window",
            "Maximized Window",
            "Windowed"
        };

        protected override void Awake()
        {
            base.Awake();

            _saveLoadSystem = new SaveLoadSystem();
            
            _masterSlider.onValueChanged.AddListener(val => OnVolumeChanged(MixerBus.Master, val, _masterValueText));
            _musicSlider.onValueChanged.AddListener(val => OnVolumeChanged(MixerBus.Music, val, _musicValueText));
            _sfxSlider.onValueChanged.AddListener(val => OnVolumeChanged(MixerBus.SFX, val, _sfxValueText));
            
            _applyButton.onClick.AddListener(ApplySettings);
            _resetButton.onClick.AddListener(ResetToDefaults);
            _backButton.onClick.AddListener(RequestClose);

            PopulateWindowModes();
            PopulateResolutions();
        }

        public override void Show()
        {
            base.Show();
            
            _workingCache = _saveLoadSystem.LoadSettings();
            RefreshUIFromCache();
        }

        public override void Hide()
        {
            base.Hide();
            _saveLoadSystem.SaveSettings(_workingCache);
        }

        private void OnVolumeChanged(MixerBus bus, float rawValue, TMP_Text label)
        {
            if (_workingCache == null)
                return;

            label.text = rawValue.ToString("0");

            float normalizedValue = rawValue / 10f;

            switch (bus)
            {
                case MixerBus.Master: _workingCache.MasterVolume = normalizedValue; break;
                case MixerBus.Music: _workingCache.MusicVolume = normalizedValue; break;
                case MixerBus.SFX: _workingCache.SFXVolume = normalizedValue; break;
            }
            
            EventBus.Publish(new AudioVolumeChangedEvent(bus, normalizedValue));
        }

        private void ApplySettings()
        {
            _workingCache.WindowMode = (FullScreenMode)_windowModeDropdown.value;
            
            Resolution selectedRes = _availableResolutions[_resolutionDropdown.value];
            _workingCache.ResolutionWidth = selectedRes.width;
            _workingCache.ResolutionHeight = selectedRes.height;
            _workingCache.RefreshRate = Mathf.RoundToInt((float)selectedRes.refreshRateRatio.value);
            
            Screen.SetResolution(
                _workingCache.ResolutionWidth,
                _workingCache.ResolutionHeight,
                _workingCache.WindowMode,
                new RefreshRate {numerator = (uint)_workingCache.RefreshRate, denominator = 1}
                );
        }

        private void ResetToDefaults()
        {
            _workingCache = new SettingsSaveData();
            RefreshUIFromCache();
            
            EventBus.Publish(new AudioVolumeChangedEvent(MixerBus.Master, _workingCache.MasterVolume));
            EventBus.Publish(new AudioVolumeChangedEvent(MixerBus.Music, _workingCache.MusicVolume));
            EventBus.Publish(new AudioVolumeChangedEvent(MixerBus.SFX, _workingCache.SFXVolume));
            
            ApplySettings();
        }

        private void RefreshUIFromCache()
        {
            float masterDisplay = _workingCache.MasterVolume * 10f;
            float musicDisplay = _workingCache.MusicVolume * 10f;
            float sfxDisplay = _workingCache.SFXVolume * 10f;

            _masterSlider.SetValueWithoutNotify(masterDisplay);
            _musicSlider.SetValueWithoutNotify(musicDisplay);
            _sfxSlider.SetValueWithoutNotify(sfxDisplay);
            
            _masterValueText.text = masterDisplay.ToString("0");
            _musicValueText.text = musicDisplay.ToString("0");
            _sfxValueText.text = sfxDisplay.ToString("0");

            _windowModeDropdown.SetValueWithoutNotify((int)_workingCache.WindowMode);

            int resIndex = 0;
            for (int i = 0; i < _availableResolutions.Length; i++)
            {
                if (_availableResolutions[i].width == _workingCache.ResolutionWidth &&
                    _availableResolutions[i].height == _workingCache.ResolutionHeight)
                {
                    resIndex = i;
                    break;
                }
            }
            
            _resolutionDropdown.SetValueWithoutNotify(resIndex);
        }

        private void PopulateWindowModes()
        {
            _windowModeDropdown.ClearOptions();
            _windowModeDropdown.AddOptions(new List<string>(_windowModeNames));
        }

        private void PopulateResolutions()
        {
            _availableResolutions = Screen.resolutions;
            _resolutionDropdown.ClearOptions();
            
            List<string> options = new List<string>();
            foreach (var res in _availableResolutions)
            {
                int hz = Mathf.RoundToInt((float)res.refreshRateRatio.value);
                options.Add($"{res.width} x {res.height} @ {hz}Hz");
            }
            
            _resolutionDropdown.AddOptions(options);
        }
    }
}