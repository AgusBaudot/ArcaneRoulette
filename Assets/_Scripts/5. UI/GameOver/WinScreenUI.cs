using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class WinScreenUI : BaseUIPanel
    {
        [SerializeField] private Button _returnToMenuButton;

        private void OnEnable()
        {
            EventBus.Subscribe<RunWonEvent>(HandleRunWon);
            _returnToMenuButton.onClick.AddListener(OnReturnClicked);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RunWonEvent>(HandleRunWon);
            _returnToMenuButton.onClick.RemoveListener(OnReturnClicked);
        }

        private void HandleRunWon(RunWonEvent evt)
        {
            Helpers.Input.EnableUIInput();

            EventBus.Publish(new AudioFilterRequest
            {
                TargetBus = MixerBus.Music,
                EnableFilter = true,
                TargetCutoffHz = 1200f,
                VolumeMultiplier = 0.4f
            });

            Time.timeScale = 0f;
            Show();
        }

        private void OnReturnClicked()
        {
            EventBus.Publish(new AudioFilterRequest
            {
                TargetBus = MixerBus.Music,
                EnableFilter = false,
                VolumeMultiplier = 1f
            });

            EventBus.Publish(new EndRunRequestEvent(SceneNames.MainMenu));
        }
    }
}