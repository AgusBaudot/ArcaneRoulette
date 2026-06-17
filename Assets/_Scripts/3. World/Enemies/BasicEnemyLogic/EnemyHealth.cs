using System;
using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    public class EnemyHealth : MonoBehaviour, IEnemyComponent, IDamageable, IElemental, IDebuffReceiver, IHealable
    {
        [Header("Stats")]
        [SerializeField] private float _maxHp; // Switched to float for clean UI division
        [SerializeField] private float _currentHp;
        [SerializeField] private bool _isDead;
        //private BlackboardKey isDead;
        private ElementType _element;

        [Header("HP Bar UI (Optional)")]
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _ghostFill;
        [SerializeField] private float _ghostSpeed = 2.5f;

        public event Action OnDeath;
        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;

        private IDebuffReadable _debuffs;
        private Blackboard _blackboard;
        private DamageFlash _flashComponent;

        public ElementType Element => _element;

        private void Awake()
        {
            _flashComponent = GetComponent<DamageFlash>();
        }
        public void Tick()
        {
            /*
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (_blackboard.TryGetValue(isDead, out bool dead))
                {
                    _blackboard.SetValue(isDead, !dead);
                    Debug.Log($"{isDead.Name}: {dead}");
                }
            }
            */


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
            Debug.Log($"Damage: {amount}. Type: {elementType}");
            _currentHp = Mathf.Max(0f, _currentHp - amount);
            UpdateUI();

            _flashComponent.Flash();

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
            _blackboard = blackboard;
            //isDead = _blackboard.GetOrRegisterKey("isDead");
            ResetComponent();
        }
        public void ResetComponent()
        {
            //_blackboard.SetValue(isDead, false);
            _isDead = false;
            _currentHp = _maxHp;
            UpdateUI();
        }

        //IDebuffReceiver Implementation------------------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}