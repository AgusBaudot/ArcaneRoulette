using UnityEngine;
using Core;

namespace World
{
    [RequireComponent(typeof(Collider))]
    public class TutorialTrapTrigger : MonoBehaviour
    {
        [SerializeField] private TutorialManager _manager;
        private bool _hasFired;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasFired) return;
            
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                _hasFired = true;
                if (_manager != null)
                {
                    _manager.OnThornTrapPassed();
                }
            }
        }
    }
}