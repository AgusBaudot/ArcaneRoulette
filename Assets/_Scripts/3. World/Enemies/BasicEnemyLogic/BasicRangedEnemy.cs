using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(Rigidbody), typeof(KnockbackHandler), typeof(EnemyHealth))]
    public class BasicRangedEnemy : BaseEnemy, IDebuffReceiver
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
            
            //Movement logic (Flee if too close)
            if (distanceToPlayer < _safeDistance)
            {
                // Move away from player
                Vector3 dirAway = (flatMyPos - flatPlayerPos).normalized;
                _rb.velocity = dirAway * finalSpeed;
            }
            else
            {
                _rb.velocity = Vector3.zero; // Stop moving, we are at a safe distance
            }

            //Shooting logic
            if (distanceToPlayer <= _aggroRange && Time.time >= _lastAttackTime + _attackCooldown / EffectiveAttackSpeed)
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
            
            Vector3 fireDir = (playerPos - GetFlatPos(transform.position)).normalized;
            if (fireDir == Vector3.zero) fireDir = transform.forward;

            var proj = Helpers.ProjFactory.Spawn(_projectilePrefab, transform.position, Quaternion.LookRotation(fireDir));
            
            // Note: Bypassing DamageSystem for now per instructions, passing Neutral
            proj.Init(fireDir, _projectileSpeed, finalDamage, ElementType.Neutral, gameObject);
        }

        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);
        
        //IDebuffReceiver Implementation-------------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}