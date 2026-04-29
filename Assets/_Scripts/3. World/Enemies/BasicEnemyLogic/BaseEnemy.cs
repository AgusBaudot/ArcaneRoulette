using UnityEngine;

namespace World
{
    public abstract class BaseEnemy : MonoBehaviour
    {
        protected Transform _playerTarget;
        
        public void Init(Transform playerTarget)
        {
            _playerTarget = playerTarget;
        }
    }
}