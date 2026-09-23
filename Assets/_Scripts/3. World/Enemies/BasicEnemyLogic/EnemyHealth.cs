using System;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    [RequireComponent(typeof(DamageFlash))]
    public class EnemyHealth : MonoBehaviour, IEnemyComponent, IDamageable, IElemental, IDebuffReceiver, IHealable
    {
        [Header("Stats")]
        [SerializeField] private float _maxHp; 
        [SerializeField] private float _currentHp;
        [SerializeField] private bool _isDead;
        private ElementType _element;

        [Header("HP Bar UI (Optional)")]
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _ghostFill;
        [SerializeField] private float _ghostSpeed = 2.5f;

        public event Action OnDeath;
        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public float DamageMitigationMultiplier { get; set; } = 1.0f;

        private IDebuffReadable _debuffs;

        public ElementType Element => _element;
        
        public void Tick()
        {
            if (_ghostFill == null || _hpFill == null)
                return;
            
            _ghostFill.fillAmount = Mathf.Lerp(_ghostFill.fillAmount, _hpFill.fillAmount, _ghostSpeed * Time.deltaTime);
        }

        public bool TakeDamage(int amount, ElementType elementType)
        {
            float finalDamage = amount * DamageMitigationMultiplier;
            _currentHp = Mathf.Max(0f, _currentHp - finalDamage);
            UpdateUI();

            if (_currentHp <= 0f)
                Die();

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
        }

        public void InitComponent(EnemyStats stats, Blackboard blackboard)
        {
            _maxHp = stats.MaxHp;
            _element = stats.ElementType;
            ResetComponent();
        }

        public void ResetComponent()
        {
            _isDead = false;
            _currentHp = _maxHp;
            DamageMitigationMultiplier = 1.0f;
            UpdateUI();
            
            // Snap the ghost bar full on spawn so it doesn't animate from 0
            if (_ghostFill != null) 
            {
                _ghostFill.fillAmount = 1f;
            }
        }

        //IDebuffReceiver Implementation------------------------
        public void RegisterDebuff(IDebuffReadable debuff)
        {
            _debuffs = debuff;
        }

        public void UnregisterDebuff()
        {
            _debuffs = null;
        }
    }
}