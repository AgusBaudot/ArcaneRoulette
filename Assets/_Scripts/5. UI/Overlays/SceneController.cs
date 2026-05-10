using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Foundation;
using World;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
   [SerializeField] private float _sceneFadeDuration;
   [SerializeField] private string _mainMenuSceneName = "MainMenu";
   [SerializeField] private int _finalRoomId = -1;

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
        EventBus.Subscribe<RoomManager.RoomClearEvent>(HandleRoomCleared);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RoomManager.RoomClearEvent>(HandleRoomCleared);
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.OnDeath -= HandlePlayerDeath;
        EventBus.Unsubscribe<RoomManager.RoomClearEvent>(HandleRoomCleared);
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

    private void HandleRoomCleared(RoomManager.RoomClearEvent evt)
    {
        if (_isLoadingMainMenu)
            return;

        bool isFinalRoom = false;
        if (_finalRoomId >= 0)
        {
            isFinalRoom = evt.roomId == _finalRoomId;
        }
        else if (_floorManager != null)
        {
            isFinalRoom = evt.roomId == _floorManager.MaximumRooms - 1;
        }

        if (!isFinalRoom)
            return;

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
