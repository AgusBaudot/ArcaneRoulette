using System;
using UnityEngine;
using World;

namespace Foundation
{
    [DefaultExecutionOrder(-500)]
    public class GameStateManager : MonoBehaviour
    {
        public static VolatileRunState RunState { get; private set; }
        
        // BUGFIX: Notify UI elements when the instance changes
        public static event Action<VolatileRunState> OnRunStateInitialized;

        private void Awake()
        {
            InitializeNewRun();
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
            EventBus.Subscribe<PlayerEnteredRoomEvent>(e => RunState.UpdatePlayerRoom(e.Index));
            EventBus.Subscribe<RoomClearEvent>(e => RunState.MarkRoomCleared(e.Index));
            EventBus.Subscribe<EndFloorClearEvent>(e => RunState.MarkRoomCleared(e.Index));
            OnRunStateInitialized?.Invoke(RunState);
        }
    }
}
