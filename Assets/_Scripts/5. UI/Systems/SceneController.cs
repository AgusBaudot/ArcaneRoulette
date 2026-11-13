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

        [Header("Portal swirl (optional — falls back to the fade if empty)")]
        [Tooltip("Must NOT be a child of the object holding this CanvasGroup (its alpha would hide the swirl).")]
        [SerializeField] private SwirlTransition _swirl;
        [SerializeField, Min(0.05f)] private float _swirlDuration = 0.9f;
        [Tooltip("Unscaled seconds to wait before the opening swirl, so the first heavy frames of a new scene don't eat the animation.")]
        [SerializeField, Min(0f)] private float _swirlOpenDelay = 0.1f;

        private CanvasGroup _canvasGroup;
        private bool _isTransitioning;

        // Survives the scene load: set by a swirl-close, consumed by the next scene's Start.
        private static bool _openWithSwirl;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _openWithSwirl = false;
        }

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
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FloorClearedEvent>(HandleFloorCleared);
            EventBus.Unsubscribe<PlayerDiedEvent>(HandlePlayerDeath);
        }

        private void Start()
        {
            bool useSwirl = _openWithSwirl && _swirl != null;
            _openWithSwirl = false;

            if (useSwirl)
            {
                // Arrived through a portal: hide the black cover, start fully swirled, unwind.
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;

                _swirl.SetCovered();
                _swirl.Open(_swirlDuration, _swirlOpenDelay);
                return;
            }

            _canvasGroup.DOFade(0f, _sceneFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => _canvasGroup.blocksRaycasts = false);
        }

        // ── Triggers ─────────────────────────────────────────────────────────

        private void HandleFloorCleared(FloorClearedEvent evt)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            string currentScene = SceneManager.GetActiveScene().name;

            CloseThen(() => EventBus.Publish(new FloorTransitionRequestEvent(currentScene)));
        }

        private void HandlePlayerDeath(PlayerDiedEvent _)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            Sequence deathSequence = DOTween.Sequence();
            deathSequence.SetUpdate(true);
            
            // Wait the 2 seconds first without blocking the screen
            deathSequence.AppendInterval(_delayBeforeDeathFade);
            
            // Apply the raycast block EXACTLY when the fade begins, matching the tutorial's coroutine order
            deathSequence.AppendCallback(() => _canvasGroup.blocksRaycasts = true);
            deathSequence.Append(_canvasGroup.DOFade(1f, _sceneFadeDuration));
            
            deathSequence.OnComplete(() => 
            {
                EventBus.Publish(new EndRunRequestEvent(_mainMenuSceneName));
            });
        }
        
        public void LoadScene(string sceneName)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            CloseThen(() => EventBus.Publish(new FloorTransitionRequestEvent(sceneName)));
        }

        // ── Transition ───────────────────────────────────────────────────────

        /// <summary>
        /// Covers the screen (swirl if assigned, black fade otherwise), then runs onClosed.
        /// A swirl-close also flags the next scene to open with the swirl.
        /// </summary>
        private void CloseThen(System.Action onClosed)
        {
            _canvasGroup.blocksRaycasts = true;

            if (_swirl != null)
            {
                _openWithSwirl = true;
                _swirl.Close(_swirlDuration)
                    .OnComplete(() => onClosed());
                return;
            }

            _canvasGroup.DOFade(1f, _sceneFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => onClosed());
        }
    }
}