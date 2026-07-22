using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PauseMenu : BaseUIPanel
    {
        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _quitButton;

        private void Awake()
        {
            base.Awake();
            
            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(OnResumeClicked);
            
            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnResumeClicked()
        {
            RequestClose();
        }

        private void OnQuitClicked()
        {
            RequestClose();
            
            EventBus.Publish(new QuitRunRequestEvent(SceneNames.MainMenu));
        }
    }
}