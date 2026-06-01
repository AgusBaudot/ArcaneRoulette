using UnityEngine;

namespace World 
{
    public class BlackboardController : MonoBehaviour , IEnemyComponent
    {
        [SerializeField] BlackboardData blackboardData;
        readonly Blackboard blackboard = new Blackboard();
        readonly Arbiter arbiter = new Arbiter();
        public Blackboard GetBlackboard() => blackboard;

        public void RegisterExpert(IExpert expert) => arbiter.RegisterExpert(expert);
        public void DeregisterExpert(IExpert expert) => arbiter.DeregisterExpert(expert);
        public void InitComponent(EnemyStats stats, Blackboard bb)
        {
            blackboardData.SetValuesOnBlackboard(blackboard);
            //blackboard.debug();
        }
        public void ResetComponent()
        {
            blackboard.Clear(); // Clean all the values
            blackboardData.SetValuesOnBlackboard(blackboard);
            //blackboard.debug();
        }
    }
}

