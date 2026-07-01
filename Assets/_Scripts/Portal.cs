using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public enum PortalType { NextFloor, StartNewRun }

    [RequireComponent(typeof(SphereCollider))]
    public class Portal : MonoBehaviour
    {
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private PortalType _type = PortalType.NextFloor;
        [SerializeField] private string _firstLevelSceneName = "Core loop";

        private bool _hasTriggered = false;

        private void Start()
        {
            var col = GetComponent<SphereCollider>();
            col.radius = _radius;
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered) return;

            if (other.TryGetComponent<PlayerHurtBox>(out _))
            {
                _hasTriggered = true;

                if (_type == PortalType.NextFloor)
                {
                    int currentRoomIndex = GameStateManager.RunState.CurrentRoomIndex;
                    EventBus.Publish(new EndFloorClearEvent(currentRoomIndex));
                }
                else if (_type == PortalType.StartNewRun)
                {
                    var gameState = FindObjectOfType<GameStateManager>();
                    
                    if (gameState != null)
                        gameState.EndRun();

                    var sceneController = FindObjectOfType<UI.SceneController>();
                    if (sceneController != null) sceneController.LoadScene(_firstLevelSceneName);
                    else UnityEngine.SceneManagement.SceneManager.LoadScene(_firstLevelSceneName);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _type == PortalType.NextFloor ? Color.cyan : Color.green;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}