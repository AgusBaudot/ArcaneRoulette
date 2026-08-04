using System;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace Meta
{
    /// <summary>
    /// Wires UI to AudioManager bus volumes.
    /// Attach to our settings panel. Each slider maps to one MixerBus.
    /// Slider must be configured: Min = 0, Max = 1, Interactable = true;
    ///
    /// Reads saved preferences on enable so sliders always reflect the
    /// current stored state when the panel opens.
    /// </summary>
    public sealed class AudioSettingsUI : MonoBehaviour
    {
        [Header("Volume Sliders (optional - leave null to skip)")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _uiSlider;
        [SerializeField] private Slider _ambienceSlider;

        private void OnEnable()
        {
            if (AudioManager.Instance == null)
                return;
            
            //Populate sliders with saved values, then subscribe.
            InitSlider(_masterSlider, MixerBus.Master);
            InitSlider(_musicSlider, MixerBus.Music);
            InitSlider(_sfxSlider, MixerBus.SFX);
            InitSlider(_uiSlider, MixerBus.UI);
            InitSlider(_ambienceSlider, MixerBus.Ambience);
            
            //Subscribe after setting initial value to avoid spurious saves on open.
            if (_masterSlider != null)
                _masterSlider.onValueChanged.AddListener(v => OnSliderChanged(MixerBus.Master, v));
            
            if (_musicSlider != null) 
                _musicSlider.onValueChanged.AddListener(v => OnSliderChanged(MixerBus.Music, v));
            
            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.AddListener(v => OnSliderChanged(MixerBus.SFX, v));
            
            if (_uiSlider != null) 
                _uiSlider.onValueChanged.AddListener(v => OnSliderChanged(MixerBus.UI, v));
            
            if (_ambienceSlider != null)
                _ambienceSlider.onValueChanged.AddListener(v => OnSliderChanged(MixerBus.Ambience, v));
        }

        private void OnDisable()
        {
            _masterSlider?.onValueChanged.RemoveAllListeners();
            _musicSlider?.onValueChanged.RemoveAllListeners();
            _sfxSlider?.onValueChanged.RemoveAllListeners();
            _uiSlider?.onValueChanged.RemoveAllListeners();
            _ambienceSlider?.onValueChanged.RemoveAllListeners();
            
            AudioManager.Instance?.CommitSettingsToDisk();
        }

        private void InitSlider(Slider slider, MixerBus bus)
        {
            if (slider == null || AudioManager.Instance == null)
                return;
            
            slider.SetValueWithoutNotify(AudioManager.Instance.GetBusVolume(bus));
        }

        private void OnSliderChanged(MixerBus bus, float value)
        {
            AudioManager.Instance?.SetBusVolume(bus, value);
        }
    }
}