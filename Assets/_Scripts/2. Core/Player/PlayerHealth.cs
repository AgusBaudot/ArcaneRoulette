using System.Collections;
using UnityEngine;
using Foundation;

namespace Core
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        public float Current {get; private set;}
        public bool IsInvincible => _iFrameTimer > 0f;

        private PlayerStats _stats;
        private float _iFrameTimer;

        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            Current = stats.BaseHp;
        }
        
        //Updatemanager: replace Update() with Tick() registration
        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (_iFrameTimer > 0f)
                _iFrameTimer -= deltaTime;
        }

        public void TakeDamage(int amount, ElementType elementType)
        {
            if (IsInvincible) return;

            Current = Mathf.Max(0f, Current - amount);
            _iFrameTimer = _stats.IFrameDuration;

            StopAllCoroutines();
            StartCoroutine(IFrameFlash());

            if (Current <= 0f) Die();
        }
        
        private IEnumerator IFrameFlash()
        {
            float elapsed = 0f;
            bool red = false;

            while (elapsed < _stats.IFrameDuration)
            {
                _spriteRenderer.color = red ? Color.red : Color.white;
                red = !red;
                //Unscaled: flash keeps running during hitstop
                yield return new WaitForSecondsRealtime(_stats.IFrameFlashInterval);
                elapsed += _stats.IFrameFlashInterval;
            }

            _spriteRenderer.color = Color.white;
        }
        
        private void Die()
        {
            //EventBus.Publish (new PlayerDiedEvent()); - wire when EventBus is ready.
        }
    }
}