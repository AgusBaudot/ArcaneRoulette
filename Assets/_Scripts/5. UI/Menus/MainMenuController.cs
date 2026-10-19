using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private AudioEventSO _menuMusic;
        
        [Header("Panels")]
        [SerializeField] private BaseUIPanel _optionsPanel;
        [SerializeField] private BaseUIPanel _settingsPanel;
        [SerializeField] private BaseUIPanel _controlsPanel;

        // ── Runtime State ────────────────────────────────────────────────────
        
        private BaseUIPanel _currentActivePanel;
        private Stack<BaseUIPanel> _panelHistory = new();

        // ── Unity ────────────────────────────────────────────────────────────

        private void Start()
        {
            if (_menuMusic != null)
            {
                EventBus.Publish(new AudioCrossfadeRequest
                {
                    NewTrack = _menuMusic,
                    Duration = 2f
                });
            }
        }
        
        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayClickedEvent>(OnPlay);
            EventBus.Subscribe<OnOptionsClickedEvent>(OnOptions);
            EventBus.Subscribe<OnSettingsClickedEvent>(OnSettings);
            EventBus.Subscribe<OnControlsClickedEvent>(OnControls);
            EventBus.Subscribe<OnExitClickedEvent>(OnExit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayClickedEvent>(OnPlay);
            EventBus.Unsubscribe<OnOptionsClickedEvent>(OnOptions);
            EventBus.Unsubscribe<OnSettingsClickedEvent>(OnSettings);
            EventBus.Unsubscribe<OnControlsClickedEvent>(OnControls);
            EventBus.Unsubscribe<OnExitClickedEvent>(OnExit);
        }

        // ── Triggers ─────────────────────────────────────────────────────────

        private void OnPlay(OnPlayClickedEvent _)
        { 
            //TODO(save system): once run persistence exists, check for a saved
            //in-progress run here and resume it instead of always starting fresh.
            
            EventBus.Publish(new StartRunRequestEvent(SceneNames.GameLevel));
        }

        private void OnOptions(OnOptionsClickedEvent _) => OpenPanel(_optionsPanel, keepHistory: false);
        
        private void OnSettings(OnSettingsClickedEvent _) => OpenPanel(_settingsPanel, keepHistory: true);
        private void OnControls(OnControlsClickedEvent _) => OpenPanel(_controlsPanel, keepHistory: true);
        
        private void OnExit(OnExitClickedEvent _) => Application.Quit();

        // ── Core Orchestration ───────────────────────────────────────────────

        private void OpenPanel(BaseUIPanel targetPanel, bool keepHistory)
        {
            if (_currentActivePanel == targetPanel) return;

            if (_currentActivePanel != null)
            {
                _currentActivePanel.OnCloseRequested -= CloseCurrentPanel;
                _currentActivePanel.Hide();

                if (keepHistory)
                {
                    _panelHistory.Push(_currentActivePanel);
                }
                else
                {
                    _panelHistory.Clear();
                }
            }

            _currentActivePanel = targetPanel;
            _currentActivePanel.OnCloseRequested += CloseCurrentPanel;
            _currentActivePanel.Show();
        }

        private void CloseCurrentPanel()
        {
            if (_currentActivePanel == null) return;

            _currentActivePanel.OnCloseRequested -= CloseCurrentPanel;
            _currentActivePanel.Hide();
            _currentActivePanel = null;

            if (_panelHistory.Count > 0)
            {
                _currentActivePanel = _panelHistory.Pop();
                _currentActivePanel.OnCloseRequested += CloseCurrentPanel;
                _currentActivePanel.Show();
            }
        }
    }
}