using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(Rigidbody), typeof(KnockbackHandler), typeof(EnemyHealth))]
    public class AdvancedRangedEnemy : BaseEnemy, IDebuffReceiver
    {
        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private int _attackDamage = 2;
        [SerializeField] private float _attackCooldown = 2f;
        
        [Header("Ranges")]
        [SerializeField] private float _aggroRange = 15f; 
        [Tooltip("Chase until this close.")]
        [SerializeField] private float _attackRange = 10f;
        [Tooltip("Flee if closer than this.")]
        [SerializeField] private float _safeDistance = 5f;

        [Header("Projectile setup")]
        [SerializeField] private EnemyProjectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 8f;
        [Tooltip("Angle between projectiles.")]
        [SerializeField] private float _spreadAngle = 15f;

        private Rigidbody _rb;
        private KnockbackHandler _knockback;
        private float _lastAttackTime;
        
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
            if (_playerTarget == null || _knockback.IsKnockedBack) return;

            Vector3 flatMyPos = GetFlatPos(transform.position);
            Vector3 flatPlayerPos = GetFlatPos(_playerTarget.position);
            float distanceToPlayer = Vector3.Distance(flatMyPos, flatPlayerPos);

            float finalSpeed = _moveSpeed;
            if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.Speed))
                finalSpeed *= 1f - _debuffs.GetDebuffStrength(DebuffType.Speed);
            
            // --- MOVEMENT STATE MACHINE ---
            if (distanceToPlayer > _aggroRange)
            {
                _rb.velocity = Vector3.zero; // Out of aggro, idle
            }
            else if (distanceToPlayer > _attackRange)
            {
                // Inside aggro, but too far to shoot -> Chase
                Vector3 dirTowards = (flatPlayerPos - flatMyPos).normalized;
                _rb.velocity = dirTowards * finalSpeed;
            }
            else if (distanceToPlayer < _safeDistance)
            {
                // Too close -> Flee
                Vector3 dirAway = (flatMyPos - flatPlayerPos).normalized;
                _rb.velocity = dirAway * finalSpeed;
            }
            else
            {
                // Inside the "Optimal Firing Band" (between safe and attack range)
                _rb.velocity = Vector3.zero; 
            }

            // --- SHOOTING LOGIC ---
            // As long as they are within attack range, they can shoot (even while fleeing!)
            if (distanceToPlayer <= _attackRange && Time.time >= _lastAttackTime + _attackCooldown / EffectiveAttackSpeed)
            {
                Shoot(flatPlayerPos);
            }
        }

        private void Shoot(Vector3 playerPos)
        {
            _lastAttackTime = Time.time;
            
            int finalDamage = _attackDamage;
            if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.ATK))
                finalDamage = Mathf.RoundToInt(finalDamage * (1f - _debuffs.GetDebuffStrength(DebuffType.ATK)));
            
            Vector3 centerDir = (playerPos - GetFlatPos(transform.position)).normalized;
            if (centerDir == Vector3.zero) centerDir = transform.forward;

            // Calculate the left and right directions using Quaternion rotation around the Y axis
            Vector3 leftDir = Quaternion.Euler(0, -_spreadAngle, 0) * centerDir;
            Vector3 rightDir = Quaternion.Euler(0, _spreadAngle, 0) * centerDir;

            // Spawn the 3 projectiles
            SpawnProjectile(leftDir, finalDamage);
            SpawnProjectile(centerDir, finalDamage);
            SpawnProjectile(rightDir, finalDamage);
        }

        private void SpawnProjectile(Vector3 direction, int damage)
        {
            var proj = Helpers.ProjFactory.Spawn(_projectilePrefab, transform.position, Quaternion.LookRotation(direction));
            proj.Init(direction, _projectileSpeed, damage, ElementType.Neutral, gameObject);
        }

        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);
        
        //IDebuffReceiver Implementation-------------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}