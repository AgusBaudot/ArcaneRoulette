using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(Rigidbody), typeof(KnockbackHandler), typeof(EnemyHealth))]
    public class BasicRangedEnemy : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private int _attackDamage = 2;
        [SerializeField] private float _attackCooldown = 2f;
        [SerializeField] private float _safeDistance = 6f; 
        [SerializeField] private float _aggroRange = 15f; 

        [Header("Projectile setup")]
        [SerializeField] private EnemyProjectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 8f;

        private Rigidbody _rb;
        private KnockbackHandler _knockback;
        private Transform _playerTarget;
        private float _lastAttackTime;

        public void Init(Transform playerTarget)
        {
            _playerTarget = playerTarget;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _knockback = GetComponent<KnockbackHandler>();
        }

        private void FixedUpdate()
        {
            if (_playerTarget == null || _knockback.IsKnockedBack) return;

            Vector3 flatMyPos = GetFlatPos(transform.position);
            Vector3 flatPlayerPos = GetFlatPos(_playerTarget.position);
            float distanceToPlayer = Vector3.Distance(flatMyPos, flatPlayerPos);

            // 1. MOVEMENT LOGIC (Flee if too close)
            if (distanceToPlayer < _safeDistance)
            {
                // Move AWAY from player
                Vector3 dirAway = (flatMyPos - flatPlayerPos).normalized;
                _rb.velocity = dirAway * _moveSpeed;
            }
            else
            {
                _rb.velocity = Vector3.zero; // Stop moving, we are at a safe distance
            }

            // 2. SHOOTING LOGIC
            if (distanceToPlayer <= _aggroRange && Time.time >= _lastAttackTime + _attackCooldown)
            {
                Shoot(flatPlayerPos);
            }
        }

        private void Shoot(Vector3 playerPos)
        {
            _lastAttackTime = Time.time;
            
            Vector3 fireDir = (playerPos - GetFlatPos(transform.position)).normalized;
            if (fireDir == Vector3.zero) fireDir = transform.forward;

            var proj = Instantiate(_projectilePrefab, transform.position, Quaternion.LookRotation(fireDir));
            
            // Note: Bypassing DamageSystem for now per instructions, passing Neutral
            proj.Init(fireDir, _projectileSpeed, _attackDamage, ElementType.Neutral, gameObject);
        }

        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);
    }
}