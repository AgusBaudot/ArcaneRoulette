using UnityEngine;
using Foundation;

namespace Core
{
    public class PlayerFeedbackSystem : MonoBehaviour
    {
        [SerializeField] private FloatingTextVFX _floatingTextPrefab;
        [SerializeField] private Transform _headSpawnPoint;

        private float _lastSpawnTime;
        private int _simultaneousCount;

        private void OnEnable()
        {
            EventBus.Subscribe<RuneClaimedEvent>(HandleRuneClaimed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RuneClaimedEvent>(HandleRuneClaimed);
        }

        private void HandleRuneClaimed(RuneClaimedEvent evt)
        {
            if (_floatingTextPrefab == null) return;

            if (Time.time - _lastSpawnTime < 0.1f) 
                _simultaneousCount++;
            else 
                _simultaneousCount = 0;
                
            _lastSpawnTime = Time.time;
            float staggerDelay = _simultaneousCount * 0.4f;

            Vector3 spawnPos = _headSpawnPoint != null 
                ? _headSpawnPoint.position 
                : transform.position + Vector3.up * 1.5f;

            var vfx = Helpers.ProjFactory.Spawn(
                _floatingTextPrefab, 
                spawnPos, 
                Quaternion.identity
            );

            vfx.Init($"{evt.RuneName} rune </color> acquired!", staggerDelay);
        }
    }
}