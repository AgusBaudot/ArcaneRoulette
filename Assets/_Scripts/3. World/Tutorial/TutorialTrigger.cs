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

            if (_teleportDestination != null)
            {
                EventBus.Publish(new PlayerTeleportRequestEvent(_teleportDestination.position));
            }
            
            if (_manager != null)
            {
                _manager.OnRoomEntered(_roomNumberToReport);
            }
        }
    }
}