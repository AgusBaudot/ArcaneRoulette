using System;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(RoomManager))]
    public sealed class RestingRoomController : MonoBehaviour
    {
        [SerializeField] private RestingStatue _lifeStatue;
        [SerializeField] private RestingStatue _greedStatue;

        private RoomManager _roomManager;

        private void Awake()
        {
            _roomManager = GetComponent<RoomManager>();
        }

        private void OnEnable()
        {
            _lifeStatue.OnStatueInteracted += HandleStatueInteracted;
            _greedStatue.OnStatueInteracted += HandleStatueInteracted;
        }

        private void OnDisable()
        {
            _lifeStatue.OnStatueInteracted -= HandleStatueInteracted;
            _greedStatue.OnStatueInteracted -= HandleStatueInteracted;
        }

        private void HandleStatueInteracted()
        {
            _lifeStatue.Deactivate();
            _greedStatue.Deactivate();
            
            _roomManager.MarkAsCleared();
        }
    }
}