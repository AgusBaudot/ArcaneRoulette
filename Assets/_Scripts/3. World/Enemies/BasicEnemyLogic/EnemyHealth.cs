using UnityEngine;
using UnityEngine.UI;
using Foundation;

namespace World
{
    public class EnemyHealth : MonoBehaviour, IDamageable, IElemental
    {
        [Header("Stats")]
        [SerializeField] private float _maxHp = 12f; // Switched to float for clean UI division
        [SerializeField] private ElementType _element = ElementType.Neutral;
        
        [Header("HP Bar UI (Optional)")]
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _ghostFill;
        [SerializeField] private float _ghostSpeed = 2.5f;

        private float _currentHp;

        public ElementType Element => _element;

        private void Awake()
        {
            _currentHp = _maxHp;
        }

        private void Update()
        {
            if (_ghostFill == null || _hpFill == null) return;
            
            // Ghost bar trails the real bar
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
            // Note: Keep your EventBus.Publish(new EnemyDiedEvent()) in mind for later!
            Destroy(gameObject);
        }
    }
}