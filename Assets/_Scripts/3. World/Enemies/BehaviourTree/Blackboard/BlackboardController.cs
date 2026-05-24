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

        void Update()
        {
            /*
            // Execute all agreed actions from the current iteration
            foreach (var action in arbiter.BlackboardIteration(blackboard))
            {
                action();
            }
            */
        }
        public void InitComponent(EnemyStats stats, Blackboard bb)
        {
            blackboardData.SetValuesOnBlackboard(blackboard);
            //blackboard.debug();
        }
        public void ResetComponent()
        {
            blackboard.Clear(); // limpiar valores actuales
            blackboardData.SetValuesOnBlackboard(blackboard);
            //Debug.Log("Variables reseteadas");
            //blackboard.debug();
        }
    }
}

