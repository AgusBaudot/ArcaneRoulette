using System;
using UnityEngine;

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
            OnRunStateInitialized?.Invoke(RunState);
        }
    }
}
