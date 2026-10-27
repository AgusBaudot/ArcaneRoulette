using System;
using System.Collections;
using UnityEngine;
using Foundation;

namespace Core
{
    public class PlayerHealth : MonoBehaviour, IUpdatable, IHealable
    {
        //IHealable
        public float CurrentHp => GameStateManager.RunState.CurrentHp;
        public float MaxHp => GameStateManager.RunState.MaxHp;
        
        public event Action OnDeath;
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
            
            EventBus.Publish(new AudioPlayRequest
            {
                Event = Helpers.PlayerAudio.TakeDamage
            });

            float newHp = CurrentHp - amount;
            GameStateManager.RunState.SetHp(newHp);
            _iFrameTimer = _stats.IFrameDuration;

            StopAllCoroutines();
            StartCoroutine(IFrameFlash());
            
            if (CurrentHp <= 0f)
                Die();

            return true;
        }

        public void Heal(float amount)
        {
            EventBus.Publish(new AudioPlayRequest
            {
                Event = Helpers.PlayerAudio.Heal
            });
            
            GameStateManager.RunState.SetHp(CurrentHp + amount);
        }
        
        private IEnumerator IFrameFlash()
        {
            float elapsed = 0f;
            bool red = false;

            while (elapsed < _stats.IFrameDuration)
            {
                _spriteRenderer.color = red ? Color.red : Color.white;
                red = !red;
                yield return new WaitForSecondsRealtime(_stats.IFrameFlashInterval);
                elapsed += _stats.IFrameFlashInterval;
            }

            _spriteRenderer.color = Color.white;
        }
        
        private void Die()
        {
            EventBus.Publish(new AudioPlayRequest
            {
                Event = Helpers.PlayerAudio.Death
            });
            
            OnDeath?.Invoke();
            EventBus.Publish (new PlayerDiedEvent());
        }
    }
}