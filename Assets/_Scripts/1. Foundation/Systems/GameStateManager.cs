using System;
using UnityEngine;
using World;

namespace Foundation
{
    [DefaultExecutionOrder(-500)]
    public class GameStateManager : MonoBehaviour
    {
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
                // We just changed floors! The state survived. 
                // Re-wire the new scene's events to the existing run state.
                SubscribeEvents();
                OnRunStateInitialized?.Invoke(RunState);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EndRunRequestEvent>(HandleRunQuit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EndRunRequestEvent>(HandleRunQuit);
        }
        
        private void HandleRunQuit(EndRunRequestEvent payload)
        {
            EndRun(payload.DestinationScene);
        }

        public void EndRun(string destinationScene)
        {
            EventBus.Publish(new AudioCrossfadeRequest { NewTrack = null, Duration = 1.0f });

            if (RunState != null)
            {
                RunState.Reset(); 
                RunState = null;
            }
            
            // EventBus.Clear();

            UnityEngine.SceneManagement.SceneManager.LoadScene(destinationScene);
        }

        private void InitializeNewRun()
        {
            RunState = new VolatileRunState(100f);
            
            // TODO: Per your conventions document, StartRun() is supposed to 
            // "seed one of each ability rune" here to give the player a starting loadout!

            SubscribeEvents();
            OnRunStateInitialized?.Invoke(RunState);
        }

        private void SubscribeEvents()
        {
            EventBus.Unsubscribe<PlayerEnteredRoomEvent>(OnPlayerEnteredRoom);
            EventBus.Unsubscribe<RoomClearEvent>(OnRoomClear);
            EventBus.Unsubscribe<EndFloorClearEvent>(OnEndFloorClear);
            EventBus.Unsubscribe<PassiveRoomClearEvent>(OnPassiveRoomClear);

            EventBus.Subscribe<PlayerEnteredRoomEvent>(OnPlayerEnteredRoom);
            EventBus.Subscribe<RoomClearEvent>(OnRoomClear);
            EventBus.Subscribe<EndFloorClearEvent>(OnEndFloorClear);
            EventBus.Subscribe<PassiveRoomClearEvent>(OnPassiveRoomClear);
        }

        // Named wrappers
        private void OnPlayerEnteredRoom(PlayerEnteredRoomEvent e) => RunState.UpdatePlayerRoom(e.Index);
        private void OnRoomClear(RoomClearEvent e) => RunState.MarkRoomCleared(e.Index);
        private void OnEndFloorClear(EndFloorClearEvent e) => RunState.MarkRoomCleared(e.Index);
        private void OnPassiveRoomClear(PassiveRoomClearEvent e) => RunState.MarkRoomCleared(e.Index);
    }
}