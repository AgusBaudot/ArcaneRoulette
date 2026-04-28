using System.Collections;
using Core;
using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(Rigidbody), typeof(KnockbackHandler), typeof(EnemyHealth))]
    public class AdvancedMeleeEnemy : BaseEnemy, IDebuffReceiver
    {
        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private int _attackDamage = 2;
        [SerializeField] private float _attackCooldown = 2f; // Now acts as recovery time after dash
        [SerializeField] private float _attackWindup = 0.5f;
        [SerializeField] private float _engageRange = 4f;    // Increased range so they dash from further away

        [Header("Dash Settings")]
        [SerializeField] private float _dashSpeed = 12f;
        [SerializeField] private float _dashDuration = 0.3f;
        [SerializeField] private float _hitboxRadius = 1.2f;

        private Rigidbody _rb;
        private KnockbackHandler _knockback;
        
        private bool _isAttacking;
        private IDebuffReadable _debuffs;
        private ContactDamage _contactDamage;

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
            _contactDamage = GetComponent<ContactDamage>();
        }

        private void FixedUpdate()
        {
            // If attacking, the coroutine handles our velocity. Skip movement logic entirely.
            if (_playerTarget == null || _isAttacking || _knockback.IsKnockedBack) 
            {
                if (!_knockback.IsKnockedBack && !_isAttacking) 
                    _rb.velocity = Vector3.zero; 
                return;
            }

            float distanceToPlayer = Vector3.Distance(GetFlatPos(transform.position), GetFlatPos(_playerTarget.position));

            if (distanceToPlayer > _engageRange)
            {
                float finalSpeed = _moveSpeed;
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.Speed))
                    finalSpeed *= 1f - _debuffs.GetDebuffStrength(DebuffType.Speed);
                
                // Chase Player
                Vector3 dir = (GetFlatPos(_playerTarget.position) - GetFlatPos(transform.position)).normalized;
                _rb.velocity = dir * finalSpeed;
            }
            else 
            {
                // In range. Halt and start the launch sequence.
                _rb.velocity = Vector3.zero;
                StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;
            _contactDamage.enabled = false;

            //Lock Direction
            Vector3 lockedDirection = Vector3.forward;
            if (_playerTarget != null)
            {
                lockedDirection = (GetFlatPos(_playerTarget.position) - GetFlatPos(transform.position)).normalized;
            }

            //Windup (Player has time to dodge out of the locked direction)
            yield return new WaitForSeconds(_attackWindup);

            //Launch Forward
            float dashTimer = 0f;
            bool hasDealtDamage = false; // Prevents hitting the player 60 times a second

            //Use FixedUpdate timing because we are forcing Rigidbody velocity
            while (dashTimer < _dashDuration)
            {
                if (_knockback.IsKnockedBack)
                    break;

                dashTimer += Time.fixedDeltaTime;
                _rb.velocity = lockedDirection * _dashSpeed;

                //Explicit hit detection during the dash
                if (!hasDealtDamage && _playerTarget != null)
                {
                    float dist = Vector3.Distance(GetFlatPos(transform.position), GetFlatPos(_playerTarget.position));
                    if (dist <= _hitboxRadius) 
                    {
                        var playerDamageable = _playerTarget.GetComponent<IDamageable>();
                        if (playerDamageable != null)
                        {
                            int finalDamage = _attackDamage;
                            if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.ATK))
                                finalDamage = Mathf.RoundToInt(finalDamage * (1f - _debuffs.GetDebuffStrength(DebuffType.ATK)));
                            
                            DamageSystem.Deal(playerDamageable, _playerTarget.gameObject, finalDamage, ElementType.Neutral);
                            hasDealtDamage = true;
                        }
                    }
                }

                yield return new WaitForFixedUpdate(); 
            }

            //Halt momentum after dash
            _rb.velocity = Vector3.zero;

            //Recovery Phase (Wait attack cooldown before moving again)
            float recoveryWait = _attackCooldown / EffectiveAttackSpeed;
            yield return new WaitForSeconds(recoveryWait);

            _isAttacking = false;
            _contactDamage.enabled = true;
        }

        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);
        
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}