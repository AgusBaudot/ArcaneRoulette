using System;
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

        private void Awake()
        {
            if (RunState == null)
            {
                InitializeNewRun();
            }
            else
            {
                SubscribeEvents();
                OnRunStateInitialized?.Invoke(RunState);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EndRunRequestEvent>(HandleRunQuit);
            EventBus.Subscribe<FloorTransitionRequestEvent>(HandleFloorTransition);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EndRunRequestEvent>(HandleRunQuit);
            EventBus.Unsubscribe<FloorTransitionRequestEvent>(HandleFloorTransition);
            UnsubscribeRoomEvents();
        }
        
        private void HandleRunQuit(EndRunRequestEvent payload)
        {
            EndRun(payload.DestinationScene);
        }

        private void HandleFloorTransition(FloorTransitionRequestEvent payload)
        {
            AdvanceFloor(payload.DestinationScene);
        }

        private void EndRun(string destinationScene)
        {
            EventBus.Publish(new AudioCrossfadeRequest { NewTrack = null, Duration = 1.0f });
            
            if (RunState != null)
            {
                RunState.Reset();
                RunState = null;
            }

            // EventBus.Clear();

            SanitizeGlobalStateAndLoad(destinationScene);
        }

        private void AdvanceFloor(string destinationScene)
        {
            SanitizeGlobalStateAndLoad(destinationScene);
        }

        private void SanitizeGlobalStateAndLoad(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Helpers.Input.EnablePlayerInput();
            
            SceneManager.LoadScene(sceneName);
        }

        private void InitializeNewRun()
        {
            RunState = new VolatileRunState(100f);
            
            Time.timeScale = 1f;
            AudioListener.pause = false;
            
            if (_startingRunes != null)
            {
                foreach (var rune in _startingRunes)
                {
                    if (rune != null)
                        RunState.AddRune(rune);
                }
            }

            SubscribeEvents();
            OnRunStateInitialized?.Invoke(RunState);
        }

        private void SubscribeEvents()
        {
            UnsubscribeRoomEvents();

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

        // Named wrappers
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