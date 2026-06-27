using Foundation;
using UnityEngine;
using DG.Tweening;

namespace World
{
    public sealed class CurrencyDrop : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _jumpPower = 1.5f;
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private AudioEventSO _gainSound;

        public void OnSpawn()
        {
        }

        // Called by DestructibleSystem.OnDeath
        public void InitDrop(Vector3 origin)
        {
            transform.position = origin;

            Vector3 randomTarget = origin + (Random.insideUnitSphere * 1.5f);
            randomTarget.y = 0f; // Keep on XZ plane

            transform.DOJump(randomTarget, _jumpPower, 1, _duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(Collect);
        }

        private void Collect()
        {
            GameStateManager.RunState.AddCurrency(1);

            if (_gainSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _gainSound, WorldPosition = transform.position });
            }

            Helpers.ProjFactory.Despawn(gameObject);
        }

        public void OnDespawn()
        {
            transform.DOKill(); // Clean up DOTween to prevent leaks on pool return
        }
    }
}