using UnityEngine;
using Core; 
using Foundation;

namespace World
{
    public sealed class MusicDirector : MonoBehaviour
    {
        [Header("Music Tracks")]
        [SerializeField] private AudioEventSO _explorationMusic;
        [SerializeField] private AudioEventSO _combatMusic;
        
        [Header("Settings")]
        [SerializeField] private float _crossfadeDuration = 1.5f;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerEnteredRoomEvent>(HandleRoomEntered);
            EventBus.Subscribe<RoomClearEvent>(HandleRoomCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerEnteredRoomEvent>(HandleRoomEntered);
            EventBus.Unsubscribe<RoomClearEvent>(HandleRoomCleared);
        }

        private void HandleRoomEntered(PlayerEnteredRoomEvent evt)
        {
            if (GameStateManager.RunState == null) return;
            if (!GameStateManager.RunState.FloorMap.TryGetValue(evt.Index, out var roomData)) return;

            bool isCombatRoom = roomData.Type == RoomType.Combat || roomData.Type == RoomType.Boss;
            
            if (isCombatRoom && !roomData.IsCleared)
            {
                EventBus.Publish(new AudioCrossfadeRequest
                {
                    NewTrack = _combatMusic,
                    Duration = _crossfadeDuration
                });
            }
            else
            {
                EventBus.Publish(new AudioCrossfadeRequest
                {
                    NewTrack = _explorationMusic,
                    Duration = _crossfadeDuration
                });
            }
        }

        private void HandleRoomCleared(RoomClearEvent evt)
        {
            EventBus.Publish(new AudioCrossfadeRequest
            {
                NewTrack = _explorationMusic,
                Duration = _crossfadeDuration
            });
        }
    }
}