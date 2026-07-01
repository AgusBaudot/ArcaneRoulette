using Foundation;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PauseMenu : MonoBehaviour
    {
        [Header("Pause Menu Overlays")]
        [SerializeField] private GameObject _pausePanel;
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _menuOpenSound;
        [SerializeField] private AudioEventSO _menuCloseSound;

        private CanvasGroup _canvasGroup;
        private bool _isOpen;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            
            _isOpen = false;
            SetOverlayState(false);
        }

        private void OnEnable()
        {
            Helpers.Input.OnPausePressed += TogglePauseMenu;
            EventBus.Subscribe<OnRunQuitEvent>(QuitToMainMenu);
            EventBus.Subscribe<OnGameResumedEvent>(ClosePauseMenu);
        }

        private void OnDisable()
        {
            Helpers.Input.OnPausePressed -= TogglePauseMenu;
            EventBus.Subscribe<OnRunQuitEvent>(QuitToMainMenu);
            EventBus.Subscribe<OnGameResumedEvent>(ClosePauseMenu);
        }

        private void TogglePauseMenu()
        {
            if (_isOpen)
                ClosePauseMenu(new OnGameResumedEvent());
            else
                OpenPauseMenu();
        }

        private void OpenPauseMenu()
        {
            _isOpen = true;
            SetOverlayState(true);

            Time.timeScale = 0f;
            AudioListener.pause = true;
            Helpers.Input.EnableUIInput();

            if (_menuOpenSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = _menuOpenSound });
        }

        public void ClosePauseMenu(OnGameResumedEvent _)
        {
            _isOpen = false;
            SetOverlayState(false);

            Time.timeScale = 1f;
            Helpers.Input.EnablePlayerInput();

            if (_menuCloseSound != null)
                EventBus.Publish(new AudioPlayRequest { Event = _menuCloseSound });
        }
        
        public void QuitToMainMenu(OnRunQuitEvent _)
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
    
            Helpers.Input.EnablePlayerInput(); 

            EventBus.Publish(new EndRunRequestEvent("Main Menu"));
        }

        private void SetOverlayState(bool isActive)
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(isActive);

            _canvasGroup.alpha = isActive ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isActive;
            _canvasGroup.interactable = isActive;
        }
    }
}