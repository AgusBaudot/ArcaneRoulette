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
        [SerializeField] private AudioEventSO _winMusic;
        [SerializeField] private AudioEventSO _loseMusic;
        
        [Header("Settings")]
        [SerializeField] private float _crossfadeDuration = 1.5f;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerEnteredRoomEvent>(HandleRoomEntered);
            EventBus.Subscribe<RoomClearEvent>(HandleRoomCleared);
            EventBus.Subscribe<PlayerDiedEvent>(HandlePlayerDeath);
            EventBus.Subscribe<FloorClearedEvent>(HandleFloorCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerEnteredRoomEvent>(HandleRoomEntered);
            EventBus.Unsubscribe<RoomClearEvent>(HandleRoomCleared);
            EventBus.Unsubscribe<PlayerDiedEvent>(HandlePlayerDeath);
            EventBus.Unsubscribe<FloorClearedEvent>(HandleFloorCleared);
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

        private void HandlePlayerDeath(PlayerDiedEvent evt)
        {
            EventBus.Publish(new AudioCrossfadeRequest
            {
                NewTrack = _loseMusic,
                Duration = 0.5f
            });
        }

        private void HandleFloorCleared(FloorClearedEvent evt)
        {
            EventBus.Publish(new AudioCrossfadeRequest
            {
                NewTrack = _winMusic,
                Duration = _crossfadeDuration
            });
        }
    }
}