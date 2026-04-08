using System;
using UnityEngine;
using UnityEngine.UI;
using Core;
using Foundation;
using TMPro;

namespace World
{
    [RequireComponent(typeof(Rigidbody))]
    public class DummyEnemy : MonoBehaviour, IDamageable, IElemental
    {
        public bool CanAttack
        {
            get => _canAttack;
            set => _canAttack = value;
        }

        public ElementType Element => _element;
        
        [Header("Stats")]
        [SerializeField] private float _maxHp = 100f;
        [SerializeField] private ElementType _element;
        
        [Header("HP Bar - assign WorldSpace Canvas children")]
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _ghostFill; //sits behind _hpFill, lerps slowly
        [SerializeField] public TextMeshProUGUI _stateText;
        [SerializeField] private float _ghostSpeed = 2.5f;

        [Header("Attack")]
        [SerializeField] private EnemyProjectile _enemyProjectilePrefab;
        [SerializeField] private bool _canAttack;
        [SerializeField] private int _damageAmount = 10;

        private float _fireInterval = 2f;
        private const float _defaultFireInterval = 2f;
        private float _currentHp;
        private Vector3 _originalPosition;

        private void Awake()
        {
            _currentHp = _maxHp;
            _originalPosition = transform.position;
            
            var rb =  GetComponent<Rigidbody>();
            rb.useGravity   = false;
            rb.constraints  = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotation;
            //this.transform.rotation.y.FreezeRotation;
        }

        private void Update()
        {
            if (_canAttack)
            {
                _fireInterval -= Time.deltaTime;
                if (_fireInterval <= 0f)
                    Fire();
            }
            
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

        private void Fire()
        {
            var projectile = Instantiate(_enemyProjectilePrefab, transform.position, Quaternion.identity);
            projectile.Init(Vector3.forward, 10f, _damageAmount, ElementType.Neutral, gameObject);

            _fireInterval = _defaultFireInterval;
        }
        
        private void Die()
        {
           //EventBus.Publish(new EnemyDiedEvent()); - wirte this when EventBus is ready
           Destroy(gameObject);
        }

        public void Reset()
        {
            _currentHp = _maxHp;
            transform.position = _originalPosition;
        }
    }
}