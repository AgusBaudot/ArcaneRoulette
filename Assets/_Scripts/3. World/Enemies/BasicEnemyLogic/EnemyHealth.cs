using System;
using UnityEngine;
using UnityEngine.UI;
using Foundation;

namespace World
{
    public class EnemyHealth : MonoBehaviour, IDamageable, IElemental, IDebuffReceiver, IHealable, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.UI;

        [Header("Stats")]
        [SerializeField] private float _maxHp = 12f; // Switched to float for clean UI division
        [SerializeField] private ElementType _element = ElementType.Neutral;
        
        [Header("HP Bar UI (Optional)")]
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _ghostFill;
        [SerializeField] private float _ghostSpeed = 2.5f;

        public event Action OnDeath;
        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        
        private float _currentHp;
        private IDebuffReadable _debuffs;
        private bool _isDead;

        public ElementType Element => _element;

        private void Awake()
        {
            _currentHp = _maxHp;
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
        }

        public void Tick(float deltaTime)
        {
            if (_ghostFill == null || _hpFill == null) return;
            
            // Ghost bar trails the real bar
            _ghostFill.fillAmount = Mathf.Lerp(
                _ghostFill.fillAmount,
                _hpFill.fillAmount,
                _ghostSpeed * Time.deltaTime
            );
        }
        
        public bool TakeDamage(int amount, ElementType elementType)
        {
            _currentHp = Mathf.Max(0f, _currentHp - amount);
            UpdateUI();
            
            if (_currentHp <= 0f) Die();
            return true;
        }
        
        public void Heal(float amount)
        {
            if (_currentHp <= 0f)
                return;

            float finalHealth = amount;
            if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.AntiHeal))
                finalHealth *= Mathf.Max(0f, 1f - _debuffs.GetDebuffStrength(DebuffType.AntiHeal));
            
            _currentHp = Mathf.Min(_maxHp, _currentHp + finalHealth);
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (_hpFill != null)
                _hpFill.fillAmount = _currentHp / _maxHp;
        }
        
        private void Die()
        {
            if (_isDead)
                return;

            _isDead = true;
            
            OnDeath?.Invoke();
            //Destroy(gameObject);
        }
        
        //IDebuffReceiver Implementation------------------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}