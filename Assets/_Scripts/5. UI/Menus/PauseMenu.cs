using UnityEngine;
using Foundation;
using UnityEngine.SceneManagement;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PauseMenu : MonoBehaviour
    {
        [Header("Pause Menu Overlays")]
        [SerializeField] private GameObject _pausePanel;

        [Tooltip("Use this to handle overlay visibility and UI input blocking")]
        [SerializeField] private CanvasGroup _canvasGroup;

        private bool _isOpen;

        private void Awake()
        {
            _isOpen = false;
            ClosePauseMenu();

            if (_canvasGroup != null)
                _canvasGroup = GetComponent<CanvasGroup>();

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

            SetOverlayState(true);

            Helpers.Input.EnableUIInput();

            Time.timeScale = 0f;
        }

        private void ClosePauseMenu()
        {
            _isOpen = false;

            Helpers.Input.EnablePlayerInput();

            SetOverlayState(false);

            Time.timeScale = 1f;
        }
        
        //Visual and interactive state of the pause menu overlay handler
        private void SetOverlayState(bool isActive)
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(isActive);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isActive ? 1f : 0f;
                _canvasGroup.blocksRaycasts = isActive;
                _canvasGroup.interactable = isActive;
            }
        }

        // Event Handlers
        private void OnGamePaused(OnGamePausedEvent _) => OpenPauseMenu();
        private void OnGameResumed(OnGameResumedEvent _) => ClosePauseMenu();
        private void OnRunQuit(OnRunQuitEvent _) => SceneManager.LoadScene("Main Menu");
    }
}

