using Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SceneController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _sceneFadeDuration = 1f;
        [SerializeField] private float _delayBeforeDeathFade = 2f;
        [SerializeField] private string _mainMenuSceneName = SceneNames.MainMenu;

        private CanvasGroup _canvasGroup;
        private bool _isTransitioning;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<FloorClearedEvent>(HandleFloorCleared);
            EventBus.Subscribe<PlayerDiedEvent>(HandlePlayerDeath);
            EventBus.Subscribe<QuitRunRequestEvent>(HandleQuitRequest);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FloorClearedEvent>(HandleFloorCleared);
            EventBus.Unsubscribe<PlayerDiedEvent>(HandlePlayerDeath);
            EventBus.Unsubscribe<QuitRunRequestEvent>(HandleQuitRequest);
        }

        private void Start()
        {
            _canvasGroup.DOFade(0f, _sceneFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => _canvasGroup.blocksRaycasts = false);
        }

        // ── Triggers ─────────────────────────────────────────────────────────

        private void HandleFloorCleared(FloorClearedEvent evt)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            
            _canvasGroup.blocksRaycasts = true;
            string currentScene = SceneManager.GetActiveScene().name;

            _canvasGroup.DOFade(1f, _sceneFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => EventBus.Publish(new FloorTransitionRequestEvent(currentScene)));
        }

        private void HandlePlayerDeath(PlayerDiedEvent _)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            _canvasGroup.blocksRaycasts = true; 

            Sequence deathSequence = DOTween.Sequence();
            deathSequence.SetUpdate(true);
            
            deathSequence.AppendInterval(_delayBeforeDeathFade);
            deathSequence.Append(_canvasGroup.DOFade(1f, _sceneFadeDuration));
            deathSequence.OnComplete(() => 
            {
                EventBus.Publish(new EndRunRequestEvent(_mainMenuSceneName));
            });
        }

        private void HandleQuitRequest(QuitRunRequestEvent evt)
        {
            if (_isTransitioning) 
                return;
            
            _isTransitioning = true;
            
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, _sceneFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => EventBus.Publish(new EndRunRequestEvent(evt.DestinationScene)));
        }
        
        public void LoadScene(string sceneName)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            
            _canvasGroup.blocksRaycasts = true;

            _canvasGroup.DOFade(1f, _sceneFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => EventBus.Publish(new FloorTransitionRequestEvent(sceneName)));
        }
    }
}