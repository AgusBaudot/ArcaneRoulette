using UnityEngine;
using Foundation;
using UnityEngine.SceneManagement;

namespace UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [Header("Pause Menu")]
        [SerializeField] private GameObject _pausePanel;

        private bool _isOpen;

        private void Awake()
        {
            _isOpen = false;
            if (_pausePanel != null)
                _pausePanel.SetActive(false);

            Time.timeScale = 1f;
            Helpers.Input.EnablePlayerInput();
        }

        private void OnEnable()
        {
            Helpers.Input.OnPausePressed += TogglePauseMenu;
            EventBus.Subscribe<OnGamePausedEvent>(OnGamePaused);
            EventBus.Subscribe<OnGameResumedEvent>(OnGameResumed);
            EventBus.Subscribe<OnRunQuitEvent>(OnRunQuit);
        }

        private void OnDisable()
        {
            Helpers.Input.OnPausePressed -= TogglePauseMenu;
            EventBus.Unsubscribe<OnGamePausedEvent>(OnGamePaused);
            EventBus.Unsubscribe<OnGameResumedEvent>(OnGameResumed);
            EventBus.Unsubscribe<OnRunQuitEvent>(OnRunQuit);
        }

        private void TogglePauseMenu()
        {
            if (_isOpen)
                EventBus.Publish<OnGameResumedEvent>(new OnGameResumedEvent());
            else
                EventBus.Publish<OnGamePausedEvent>(new OnGamePausedEvent());
        }

        private void OpenPauseMenu()
        {
            _isOpen = true;

            if (_pausePanel != null)
                _pausePanel.SetActive(true);

            Time.timeScale = 0f;
        }

        private void ClosePauseMenu()
        {
            _isOpen = false;

            if (_pausePanel != null)
                _pausePanel.SetActive(false);

            Time.timeScale = 1f;
        }
        

        private void OnGamePaused(OnGamePausedEvent _) => OpenPauseMenu();
        private void OnGameResumed(OnGameResumedEvent _) => ClosePauseMenu();
        private void OnRunQuit(OnRunQuitEvent _) => SceneManager.LoadScene(1);
    }
}

