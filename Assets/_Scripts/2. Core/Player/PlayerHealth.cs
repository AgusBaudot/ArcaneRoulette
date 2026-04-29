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
        
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.85f;
        [SerializeField] private Transform _heartsContainer;
        
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
            UpdateUI();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            GameStateManager.RunState.OnHpChanged += OnHpChanged;
        }

        private void OnDisable()
        {
            GameStateManager.RunState.OnHpChanged -= OnHpChanged;
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

        private void OnHpChanged(float current, float max)
        {
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            float current = Current;
            float max = GameStateManager.RunState.MaxHp;
            for(int i = 0; i < _heartsContainer.childCount; i++)
            {
                Transform child = _heartsContainer.GetChild(i);
                bool active = i < max;
                child.gameObject.SetActive(active);
                if (active)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        float fill = Mathf.Clamp01(current - i);
                        img.fillAmount = fill;
                    }
                }
            }
        }
    }
}