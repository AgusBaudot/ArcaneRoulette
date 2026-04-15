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
        [SerializeField] private Transform _heartsContainer;
        
        //IUpdatable
        public int UpdatePriority => Foundation.UpdatePriority.Player; 
            
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        private PlayerStats _stats;
        private float _iFrameTimer;

        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            Current = stats.BaseHp;
            
            PopulateHUD();
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
            
            if (_heartsContainer.transform.childCount > Current)
            {
                for (int i = 0; i < _heartsContainer.transform.childCount; i++)
                {
                    // If the index is less than Current, it stays on. Otherwise, it turns off.
                    _heartsContainer.transform.GetChild(i).gameObject.SetActive(i < Current);
                }
            }

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
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            
            transform.position = Vector3.zero;
            Current = _stats.BaseHp;
            
            //EventBus.Publish (new PlayerDiedEvent()); - wire when EventBus is ready.
        }

        private void PopulateHUD()
        {
            for (int i = 0; i < Current; i++)
            {
                _heartsContainer.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }
}