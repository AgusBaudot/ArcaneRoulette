using UnityEngine;

namespace Foundation
{
    [DefaultExecutionOrder(-500)]
    public class GameStateManager : MonoBehaviour
    {
        public static VolatileRunState RunState { get; private set; }

        private void Awake()
        {
            RunState = new VolatileRunState(100f);
        }

        public void EndRun()
        {
            RunState.Reset();
            EventBus.Clear();
            RunState = new  VolatileRunState(100f); //Fresh instance next run
        }
    }
}