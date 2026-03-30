using System;
using UnityEngine;
using UnityEngine.UI;
using Core;
using Foundation;
using TMPro;

namespace world
{
    [RequireComponent(typeof(Rigidbody))]
    
    public class DummyEnemy : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float _maxHp = 100f;
        
        [Header("HP Bar - assign WorldSpace Canvas children")]
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _ghostFill; //sits behind _hpFill, lerps slowly
        [SerializeField] public TextMeshProUGUI _stateText;
        [SerializeField] private float _ghostSpeed = 2.5f;

        private float _currentHp;

        private void Awake()
        {
            _currentHp = _maxHp;
            var rb =  GetComponent<Rigidbody>();
            rb.useGravity   = false;
            rb.constraints  = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            if (_ghostFill == null) return;
            //Ghost bar trails the real bar - the gap is the "damage taken" read
            _ghostFill.fillAmount = Mathf.Lerp(
                _ghostFill.fillAmount,
                _hpFill.fillAmount,
                _ghostSpeed * Time.deltaTime
            );
        }

        public void TakeDamage(int amount, ElementType elementType)
        {
            _currentHp = Mathf.Max(0f, _currentHp - amount);
            if (_hpFill != null)
                _hpFill.fillAmount = _currentHp / _maxHp;

            if (_currentHp <= 0f) Die();
        }
        
        private void Die()
        {
           //EventBus.Publish(new EnemyDiedEvent()); - wirte this when EventBus is ready
           Destroy(gameObject);
        }
    }
}