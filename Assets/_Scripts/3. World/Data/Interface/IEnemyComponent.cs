using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace World 
{
    public interface IEnemyComponent
    {
        /// An interface that simplifies and clarifies the logic of an Enemy's components
        public void InitComponent(EnemyStats stats, Blackboard bb);
        public void ResetComponent();
    }
}
