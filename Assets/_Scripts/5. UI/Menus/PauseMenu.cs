using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;
using UnityEngine.SceneManagement;


namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<OnGamePausedEvent>(OnGamePaused);
            EventBus.Subscribe<OnGameResumedEvent>(OnGameResumed);
            EventBus.Subscribe<OnRunQuitEvent>(OnRunQuit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnGamePausedEvent>(OnGamePaused);
            EventBus.Unsubscribe<OnGameResumedEvent>(OnGameResumed);
            EventBus.Unsubscribe<OnRunQuitEvent>(OnRunQuit);
        }

        private void OnGamePaused(OnGamePausedEvent _) => Time.timeScale = 0f;
        private void OnGameResumed(OnGameResumedEvent _) => Time.timeScale = 1f;
        private void OnRunQuit(OnRunQuitEvent _) => SceneManager.LoadScene(1);



    }
}

