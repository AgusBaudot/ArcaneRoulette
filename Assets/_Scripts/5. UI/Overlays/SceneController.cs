using System.Collections;
using Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private float _sceneFadeDuration = 1f;
        [SerializeField] private float _delayBeforeDeathFade = 2f;
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        private SceneFade _sceneFade;
        private bool _isTransitioning;

        private void Awake()
        {
            _sceneFade = GetComponentInChildren<SceneFade>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EndFloorClearEvent>(HandleFloorCleared);
            EventBus.Subscribe<PlayerDiedEvent>(HandlePlayerDeath);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EndFloorClearEvent>(HandleFloorCleared);
            EventBus.Unsubscribe<PlayerDiedEvent>(HandlePlayerDeath);
        }

        private IEnumerator Start()
        {
            yield return StartCoroutine(_sceneFade.FadeIn(_sceneFadeDuration));
        }

        private void HandleFloorCleared(EndFloorClearEvent evt)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            
            StartCoroutine(LoadSceneCoroutine(SceneManager.GetActiveScene().name));
        }

        private void HandlePlayerDeath(PlayerDiedEvent _)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            StartCoroutine(DeathTransitionCoroutine());
        }

        private IEnumerator DeathTransitionCoroutine()
        {
            yield return CoroutineUtils.GetWait(_delayBeforeDeathFade);

            yield return StartCoroutine(_sceneFade.FadeOut(_sceneFadeDuration));

            EventBus.Publish(new EndRunRequestEvent(_mainMenuSceneName));
        }

        public void LoadScene(string sceneName)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            yield return StartCoroutine(_sceneFade.FadeOut(_sceneFadeDuration));
            SceneManager.LoadScene(sceneName);
        }
    }
}