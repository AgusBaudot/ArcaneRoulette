using UnityEngine;
using UnityEngine.SceneManagement;
using Foundation;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayClickedEvent>(OnPlay);
            EventBus.Subscribe<OnSettingsClickedEvent>(OnSettings);
            EventBus.Subscribe<OnExitClickedEvent>(OnExit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayClickedEvent>(OnPlay);
            EventBus.Unsubscribe<OnSettingsClickedEvent>(OnSettings);
            EventBus.Unsubscribe<OnExitClickedEvent>(OnExit);
        }

        private void OnPlay(OnPlayClickedEvent _) => SceneManager.LoadScene(1);
        private void OnSettings(OnSettingsClickedEvent _) => Debug.Log("OnSettingsClickedEvent");
        private void OnExit(OnExitClickedEvent _) => Application.Quit();
    }
}