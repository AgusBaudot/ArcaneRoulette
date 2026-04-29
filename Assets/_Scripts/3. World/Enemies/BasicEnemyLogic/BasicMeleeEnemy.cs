using System.Collections;
using Core;
using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(Rigidbody), typeof(KnockbackHandler), typeof(EnemyHealth))]
    public class BasicMeleeEnemy : BaseEnemy, IDebuffReceiver
    {
        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private int _attackDamage = 2;
        [SerializeField] private float _attackCooldown = 2f;
        [SerializeField] private float _attackWindup = 0.5f;
        [SerializeField] private float _meleeRange = 1.5f;

        private Rigidbody _rb;
        private KnockbackHandler _knockback;
        
        private float _lastAttackTime;
        private bool _isAttacking;

        private IDebuffReadable _debuffs;
        
        private float EffectiveAttackSpeed
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.AttackSpeed))
                {
                    return Mathf.Max(0.1f, 1f - _debuffs.GetDebuffStrength(DebuffType.AttackSpeed));
                }

                return 1f;
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _knockback = GetComponent<KnockbackHandler>();
        }

        private void FixedUpdate()
        {
            if (_playerTarget == null || _isAttacking || _knockback.IsKnockedBack) 
            {
                if (!_knockback.IsKnockedBack && !_isAttacking) 
                    _rb.velocity = Vector3.zero; // Stop moving if no target/attacking
                return;
            }

            float distanceToPlayer = Vector3.Distance(GetFlatPos(transform.position), GetFlatPos(_playerTarget.position));

            if (distanceToPlayer > _meleeRange)
            {
                float finalSpeed = _moveSpeed;
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.Speed))
                    finalSpeed *= 1f - _debuffs.GetDebuffStrength(DebuffType.Speed);
                
                // Chase Player
                Vector3 dir = (GetFlatPos(_playerTarget.position) - GetFlatPos(transform.position)).normalized;
                _rb.velocity = dir * finalSpeed;
            }
            else if (Time.time >= _lastAttackTime + _attackCooldown / EffectiveAttackSpeed)
            {
                // In range and ready to attack
                _rb.velocity = Vector3.zero;
                StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;

            // Wait for windup
            yield return new WaitForSeconds(_attackWindup);

            // Re-check distance after windup (did the player dodge?)
            if (_playerTarget != null)
            {
                float distance = Vector3.Distance(GetFlatPos(transform.position), GetFlatPos(_playerTarget.position));
                if (distance <= _meleeRange + 0.5f) // Small buffer so it doesn't miss unfairly
                {
                    var playerDamageable = _playerTarget.GetComponent<IDamageable>();
                    if (playerDamageable != null)
                    {
                        int finalDamage = _attackDamage;
                        if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.ATK))
                            finalDamage = Mathf.RoundToInt(finalDamage * (1f - _debuffs.GetDebuffStrength(DebuffType.ATK)));
                        
                        // Melee is usually physical/neutral
                        // playerDamageable.TakeDamage(_attackDamage, ElementType.Neutral); 
                        DamageSystem.Deal(playerDamageable, _playerTarget.gameObject, finalDamage, ElementType.Neutral);
                    }
                }
            }

            _lastAttackTime = Time.time;
            _isAttacking = false;
        }

        // Helper to ensure we stay strictly on the XZ plane for distance calculations
        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);
        
        //IDebuffReceiver Implementation --------------------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}