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
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _deathSound;

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
            if (DamageMitigationMultiplier < 1.0f)
            {
                GetComponent<RangeAIBrain>()?.OnBlockHit();
            }

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

            if (_deathSound != null)
            {
                EventBus.Publish(new AudioPlayRequest {Event = _deathSound, WorldPosition = transform.position});
            }

            OnDeath?.Invoke();
            
            GameStateManager.RunState?.FireKill(default(KillContext));
        }

        public void InitComponent(EnemyStats stats, Blackboard blackboard)
        {
            _maxHp = stats.MaxHp;
            ResetComponent();
        }

        public void ResetComponent()
        {
            _isDead = false;
            _currentHp = _maxHp;
            DamageMitigationMultiplier = 1.0f;
            UpdateUI();
            
            if (_ghostFill != null) 
            {
                _ghostFill.fillAmount = 1f;
            }
        }

        public void RegisterDebuff(IDebuffReadable debuff)
        {
            _debuffs = debuff;
        }

        public void UnregisterDebuff()
        {
            _debuffs = null;
        }
        
        public void SetRuntimeElement(ElementType newElement)
        {
            _element = newElement;
        }
    }
}