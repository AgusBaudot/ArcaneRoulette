using UnityEngine;
using Core;
using Foundation;
using World;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private TutorialManager _manager;
    [SerializeField] private int _roomNumberToReport;
    [SerializeField] private Transform _teleportDestination; 
    
    private bool _hasFired;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasFired) return;
        
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            _hasFired = true;

            if (_manager != null && _teleportDestination != null)
            {
                _manager.TransitionToRoom(_roomNumberToReport, _teleportDestination.position);
            }
        }
    }
}