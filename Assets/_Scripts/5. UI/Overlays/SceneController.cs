using System.Collections;
using Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private float _sceneFadeDuration = 1f;
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
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EndFloorClearEvent>(HandleFloorCleared);
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

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            yield return StartCoroutine(_sceneFade.FadeOut(_sceneFadeDuration));
            SceneManager.LoadScene(sceneName);
        }
    }
}