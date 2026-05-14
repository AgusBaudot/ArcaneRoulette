using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Foundation;

namespace Core
{
    public class PlayerHealth : MonoBehaviour, IUpdatable
    {
        public event Action OnDeath;
        public float Current => GameStateManager.RunState.CurrentHp;
        public bool IsInvincible => _iFrameTimer > 0f;
        
        //IUpdatable
        public int UpdatePriority => Foundation.UpdatePriority.Player; 
            
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        private PlayerStats _stats;
        private float _iFrameTimer;
        
        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            GameStateManager.RunState.SetMaxHp(_stats.BaseHp);
            GameStateManager.RunState.SetHp(_stats.BaseHp);
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

        public bool TakeDamage(int amount, ElementType elementType)
        {
            if (IsInvincible)
                return false;

            float newHp = Current - amount;
            GameStateManager.RunState.SetHp(newHp);
            _iFrameTimer = _stats.IFrameDuration;

            StopAllCoroutines();
            StartCoroutine(IFrameFlash());
            
            if (Current <= 0f)
                Die();
                
            return true;
        }

        public void Heal(int amount)
            => GameStateManager.RunState.SetHp(Current + amount);
        
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
            OnDeath?.Invoke();
            //EventBus.Publish (new PlayerDiedEvent()); - wire when EventBus is ready.
        }
    }
}