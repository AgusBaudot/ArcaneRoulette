using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
   [SerializeField] private float _sceneFadeDuration;
   [SerializeField] private string _mainMenuSceneName = "MainMenu";

   private SceneFade _sceneFade;
   private PlayerHealth _playerHealth;
   private bool _isLoadingMainMenu;

   private void Awake()
   {
        _sceneFade = GetComponentInChildren<SceneFade>();
        _playerHealth = FindObjectOfType<PlayerHealth>();
        if (_playerHealth != null)
            _playerHealth.OnDeath += HandlePlayerDeath;
   }

    private void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.OnDeath -= HandlePlayerDeath;
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
        StartCoroutine(LoadMainMenuOnDeath());
    }

    private IEnumerator LoadMainMenuOnDeath()
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
