using System;
using System.Collections;
using UnityEngine;
using Foundation;

namespace Core
{
    public class PlayerHealth : MonoBehaviour, IDamageable, IUpdatable
    {
        public float Current {get; private set;}
        public bool IsInvincible => _iFrameTimer > 0f;
        
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.85f;
        
        //IUpdatable
        public int UpdatePriority => Foundation.UpdatePriority.Player; 
            
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        private PlayerStats _stats;
        private float _iFrameTimer;

        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            Current = stats.BaseHp;
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
        }

        public void Tick(float dt)
        {
            if (_iFrameTimer > 0f)
                _iFrameTimer -= dt;
        }

        public void TakeDamage(int amount, ElementType elementType)
        {
            if (IsInvincible) return;
            
            //Juice
            CameraShake.AddTrauma(_cameraTrauma);
            HitStop.Apply(_hitStopDuration);

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