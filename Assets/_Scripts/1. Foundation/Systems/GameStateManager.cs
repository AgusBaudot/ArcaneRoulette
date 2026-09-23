using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;

namespace Foundation
{
    [DefaultExecutionOrder(-500)]
    public class GameStateManager : MonoBehaviour
    {
        [Header("Starting Loadout")] [SerializeField]
        private AbilityRuneSO[] _startingRunes;

        public static VolatileRunState RunState { get; private set; }
        public static event Action<VolatileRunState> OnRunStateInitialized;

        private static GameStateManager _instance;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EventBus.Subscribe<StartRunRequestEvent>(HandleStartRun);
            EventBus.Subscribe<EndRunRequestEvent>(HandleEndRun);
            EventBus.Subscribe<FloorTransitionRequestEvent>(HandleFloorTransition);
            SubscribeRoomEvents();

            SceneManager.sceneLoaded += HandleSceneLoaded;

            // Supports hitting Play directly in a gameplay/test scene, skipping
            // MainMenu entirely. Only relevant on this very first Awake — DontDestroyOnLoad
            // means Awake never fires again for this instance, so this can't misfire
            // later just because some scene happens to load with RunState temporarily null.
            if (SceneManager.GetActiveScene().name != SceneNames.MainMenu && RunState == null)
            {
                StartNewRun();
            }
        }

        private void OnDestroy()
        {
            if (_instance != this) return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            EventBus.Unsubscribe<StartRunRequestEvent>(HandleStartRun);
            EventBus.Unsubscribe<EndRunRequestEvent>(HandleEndRun);
            EventBus.Unsubscribe<FloorTransitionRequestEvent>(HandleFloorTransition);
            UnsubscribeRoomEvents();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RunState?.InitializeFloorMap(new Dictionary<int, VolatileRunState.RoomMapData>());
        }

        private void HandleStartRun(StartRunRequestEvent payload)
        {
            StartNewRun();
            SanitizeGlobalStateAndLoad(payload.DestinationScene);
        }

        private void StartNewRun()
        {
            RunState?.Reset();
            RunState = new VolatileRunState(100f);

            if (_startingRunes != null)
            {
                foreach (var rune in _startingRunes)
                    if (rune != null)
                        RunState.AddRune(rune);
            }

            OnRunStateInitialized?.Invoke(RunState);
        }

        private void HandleEndRun(EndRunRequestEvent payload)
        {
            EventBus.Publish(new AudioCrossfadeRequest { NewTrack = null, Duration = 1.0f });

            if (RunState != null)
            {
                RunState.Reset();
                RunState = null;
            }

            SanitizeGlobalStateAndLoad(payload.DestinationScene);
        }

        private void HandleFloorTransition(FloorTransitionRequestEvent payload)
        {
            SanitizeGlobalStateAndLoad(payload.DestinationScene);
        }

        private void SanitizeGlobalStateAndLoad(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Helpers.Input.EnablePlayerInput();

            SceneManager.LoadScene(sceneName);
        }

        private void SubscribeRoomEvents()
        {
            EventBus.Subscribe<PlayerEnteredRoomEvent>(OnPlayerEnteredRoom);
            EventBus.Subscribe<RoomClearEvent>(OnRoomClear);
            EventBus.Subscribe<EndFloorClearEvent>(OnEndFloorClear);
            EventBus.Subscribe<PassiveRoomClearEvent>(OnPassiveRoomClear);
            EventBus.Subscribe<FloorClearedEvent>(OnFloorCleared);
        }

        private void UnsubscribeRoomEvents()
        {
            EventBus.Unsubscribe<PlayerEnteredRoomEvent>(OnPlayerEnteredRoom);
            EventBus.Unsubscribe<RoomClearEvent>(OnRoomClear);
            EventBus.Unsubscribe<EndFloorClearEvent>(OnEndFloorClear);
            EventBus.Unsubscribe<PassiveRoomClearEvent>(OnPassiveRoomClear);
            EventBus.Unsubscribe<FloorClearedEvent>(OnFloorCleared);
        }

        private void OnPlayerEnteredRoom(PlayerEnteredRoomEvent e) => RunState.UpdatePlayerRoom(e.Index);
        private void OnRoomClear(RoomClearEvent e) => RunState.MarkRoomCleared(e.Index);
        private void OnEndFloorClear(EndFloorClearEvent e) => RunState.MarkRoomCleared(e.Index);
        private void OnPassiveRoomClear(PassiveRoomClearEvent e) => RunState.MarkRoomCleared(e.Index);

        private void OnFloorCleared(FloorClearedEvent e)
        {
            if (RunState == null)
            {
                Debug.LogWarning("FloorClearedEvent received with no active RunState");
                return;
            }
            RunState.CurrentFloor++;
        }
    }
}