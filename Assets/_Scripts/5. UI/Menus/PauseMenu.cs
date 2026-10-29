using Foundation;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PauseMenu : BaseUIPanel
    {
        [Header("Animation")]
        [Tooltip("Assign the actual menu window child object here.")]
        [SerializeField] private RectTransform _visualRoot;
        
        [Tooltip("How long the slide animation takes.")]
        [SerializeField] private float _animationDuration = 0.4f;
        [Tooltip("The strength of the bounce/elastic effect.")]
        [SerializeField] private float _overshootAmount = 1.2f;
        [Tooltip("How far down (in Canvas units) the panel starts/ends.")]
        [SerializeField] private float _hiddenOffsetY = 1200f;

        [Header("Buttons")] 
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        private Vector2 _originalPosition;

        protected override void Awake()
        {
            base.Awake(); 

            if (_visualRoot != null)
            {
                _originalPosition = _visualRoot.anchoredPosition;
        
                _visualRoot.anchoredPosition = new Vector2(_originalPosition.x, _originalPosition.y - _hiddenOffsetY);
            }

            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(OnResumeClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);
        }

        public override void Show()
        {
            base.Show(); 
            
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.anchoredPosition = new Vector2(_originalPosition.x, _originalPosition.y - _hiddenOffsetY);
                _visualRoot.DOAnchorPos(_originalPosition, _animationDuration)
                    .SetEase(Ease.OutBack, _overshootAmount)
                    .SetUpdate(true);
            }
        }

        public override void Hide()
        {
            base.Hide();

            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOAnchorPos(new Vector2(_originalPosition.x, _originalPosition.y - _hiddenOffsetY), _animationDuration)
                    .SetEase(Ease.InBack, _overshootAmount)
                    .SetUpdate(true);
            }
        }

        private void OnResumeClicked()
        {
            RequestClose();
        }

        private void OnSettingsClicked()
        {
            EventBus.Publish(new SettingsOpenRequestEvent());
        }

        private void OnQuitClicked()
        {
            RequestClose();
            EventBus.Publish(new EndRunRequestEvent(SceneNames.MainMenu));
        }
    }
}