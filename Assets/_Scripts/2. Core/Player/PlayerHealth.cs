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
        [Tooltip("0 = 1/4, 1 = 1/2, 2 = 3/4, 3 = Full")]
        [SerializeField] private Sprite[] _heartSprites;
        
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
            for (int i = 0; i < _heartsContainer.childCount; i++)
            {
                Transform child = _heartsContainer.GetChild(i);
        
                // Only activate children up to the currently needed visible hearts
                bool active = i < Mathf.CeilToInt((int)Current / 4f);
                child.gameObject.SetActive(active);

                if (active)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        // Calculate how much HP belongs in this specific heart (from 1 to 4)
                        int hpInThisHeart = Mathf.Clamp((int)Current - (i * 4), 1, 4);

                        // Subtract 1 because your array is size 4 (1 HP = index 0, 4 HP = index 3)
                        img.sprite = _heartSprites[hpInThisHeart - 1];
                    }
                }
            }
            
            //IF WE EVER HAVE EMPTY HEARTS ---------------------------------------
            
            // int currentHp = (int)Current;
            //
            // // Divide max HP by 4 to get total containers (e.g., 12 max HP = 3 hearts)
            // int totalHeartContainers = Mathf.CeilToInt(GameStateManager.RunState.MaxHp / 4f);
            //
            // for (int i = 0; i < _heartsContainer.childCount; i++)
            // {
            //     Transform child = _heartsContainer.GetChild(i);
            //
            //     // Only activate children up to our max heart containers
            //     bool active = i < totalHeartContainers;
            //     child.gameObject.SetActive(active);
            //
            //     if (active)
            //     {
            //         Image img = child.GetComponent<Image>();
            //         if (img != null)
            //         {
            //             // Calculate how much HP belongs in this specific heart (from 0 to 4)
            //             int hpInThisHeart = Mathf.Clamp(currentHp - (i * 4), 0, 4);
            //
            //             img.sprite = _heartSprites[hpInThisHeart];
            //         }
            //     }
            // }
        }
    }
}