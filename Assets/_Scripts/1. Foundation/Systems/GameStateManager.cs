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
                SubscribeEvents();
                OnRunStateInitialized?.Invoke(RunState);
            }
        }

        public void EndRun()
        {
            RunState?.Reset();
            EventBus.Clear();
            InitializeNewRun();
        }

        private void InitializeNewRun()
        {
            RunState = new VolatileRunState(100f);
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