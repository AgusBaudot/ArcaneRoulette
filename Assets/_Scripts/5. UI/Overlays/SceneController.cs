using System.Collections;
using Core;
using Foundation;
using World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private float _sceneFadeDuration;
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        private SceneFade _sceneFade;
        private PlayerHealth _playerHealth;
        private FloorManager _floorManager;
        private bool _isLoadingMainMenu;

        private void Awake()
        {
            _sceneFade = GetComponentInChildren<SceneFade>();
            _playerHealth = FindObjectOfType<PlayerHealth>();
            _floorManager = FindObjectOfType<FloorManager>();
            if (_playerHealth != null)
                _playerHealth.OnDeath += HandlePlayerDeath;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EndFloorClearEvent>(HandleRoomCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EndFloorClearEvent>(HandleRoomCleared);
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
                _playerHealth.OnDeath -= HandlePlayerDeath;
            EventBus.Unsubscribe<EndFloorClearEvent>(HandleRoomCleared);
        }

        private IEnumerator Start()
        {
            yield return StartCoroutine(_sceneFade.FadeIn(_sceneFadeDuration));
        }

        private void HandlePlayerDeath()
        {
            if (_isLoadingMainMenu)
                return;

            _isLoadingMainMenu = true;
            StartCoroutine(LoadMainMenuCoroutine());
        }

        private void HandleRoomCleared(EndFloorClearEvent evt)
        {
            if (_isLoadingMainMenu) return;
            if (_floorManager == null) return;
            _isLoadingMainMenu = true;
            StartCoroutine(LoadMainMenuCoroutine());
        }

        private IEnumerator LoadMainMenuCoroutine()
        {
            yield return StartCoroutine(_sceneFade.FadeOut(_sceneFadeDuration));
            SceneManager.LoadScene(_mainMenuSceneName);
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

