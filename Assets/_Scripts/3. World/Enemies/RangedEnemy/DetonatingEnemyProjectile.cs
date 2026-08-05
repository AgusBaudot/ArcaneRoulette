using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public sealed class DetonatingEnemyProjectile : BaseProjectile, IEnemyProjectile
    {
        [Header("Detonation Settings")]
        // FIXED: Strongly typed so it matches Spawn<T> generic constraints
        [SerializeField] private EnemyProjectile _normalProjectilePrefab; 
        
        public override bool IsEnemy => true;
        public override ElementType SpellElement => _element;
        public ElementType Element => _element;
        public int Damage { get; private set; }
        public GameObject Owner { get; private set; }
        
        private ElementType _element;
        private float _drainRate;
        private float _timer;
        private Vector3 _fireDirection;
        private bool _isDetonating;

        public void InitBig(Vector3 dir, float speed, int damage, ElementType element, GameObject owner, float drainRate)
        {
            _fireDirection = dir;
            Damage = damage;
            _element = element;
            Owner = owner;
            _drainRate = drainRate;
            _timer = 6f; 
            _isDetonating = false;
            BounceCount = 0;

            SetVelocity(dir, speed);
            PlayParticles();
        }

        private void FixedUpdate()
        {
            if (_isDetonating) return;

            float newSpeed = Mathf.Max(0, Speed - (_drainRate * Time.fixedDeltaTime));
            SetVelocity(_fireDirection, newSpeed);

            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0)
            {
                Detonate();
            }
        }

        protected override void OnHitDamageable(Collider other)
        {
            if (_isDetonating) return;

            var damageable = other.GetComponent<IDamageable>();
            bool isPlayer = other.GetComponentInParent<PlayerController>() != null;

            if (isPlayer && damageable != null)
            {
                var batch = new DamageBatch();
                batch.Deal(damageable, Damage, ElementType.Neutral);
                batch.Commit(Helpers.Combat.PlayerDamage);
                
                Detonate();
            }
        }
        
        protected override void OnHitWall(Collider other)
        {
            if (_isDetonating) return;
            Detonate(); 
        }

        private void Detonate()
        {
            if (_isDetonating) return;
            _isDetonating = true;

            float[] angles = { 0f, 90f, 180f, 270f };
            foreach (float angle in angles)
            {
                Vector3 dir = Quaternion.Euler(0, angle, 0) * _fireDirection;
                
                // FIXED: _normalProjectilePrefab is now correctly of type EnemyProjectile
                var proj = Helpers.ProjFactory.Spawn<EnemyProjectile>(_normalProjectilePrefab, transform.position, Quaternion.LookRotation(dir));
                
                proj.Init(dir, 12f, Damage, Element, Owner); 
            }

            SpawnImpactVFX(); 
            Helpers.ProjFactory.Despawn(gameObject);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            Owner = null;
        }
    }
}