using Foundation;
using UnityEngine;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _settingsPanel;
        
        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayClickedEvent>(OnPlay);
            EventBus.Subscribe<OnSettingsClickedEvent>(OnSettings);
            EventBus.Subscribe<OnExitClickedEvent>(OnExit);
            EventBus.Subscribe<OnSettingsUIClosedEvent>(OnSettingsClosed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayClickedEvent>(OnPlay);
            EventBus.Unsubscribe<OnSettingsClickedEvent>(OnSettings);
            EventBus.Unsubscribe<OnExitClickedEvent>(OnExit);
            EventBus.Unsubscribe<OnSettingsUIClosedEvent>(OnSettingsClosed);
        }

        private void OnPlay(OnPlayClickedEvent _) => EventBus.Publish(new EndRunRequestEvent(SceneNames.Lobby));

        private void OnSettings(OnSettingsClickedEvent _)
            => _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        
        private void OnSettingsClosed(OnSettingsUIClosedEvent _)
            => _settingsPanel.SetActive(false);
        
        private void OnExit(OnExitClickedEvent _) => Application.Quit();
    }
}