using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public sealed class DetonatingEnemyProjectile : BaseProjectile, IEnemyProjectile, ICustomReflectable
    {
        [Header("Detonation Settings")]
        [SerializeField] private EnemyProjectile _normalProjectilePrefab; 
        
        public override bool IsEnemy => _isEnemy;
        public override ElementType SpellElement => _element;
        public ElementType Element => _element;
        public int Damage { get; private set; }
        public GameObject Owner { get; private set; }
        
        private ElementType _element;
        private float _drainRate;
        private float _timer;
        private Vector3 _fireDirection;
        private bool _isDetonating;
        private bool _isEnemy = true;

        public void InitBig(Vector3 dir, float speed, int damage, ElementType element, GameObject owner, float drainRate)
        {
            _isEnemy = true;
            gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
            
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
            if (damageable == null) return;

            bool isPlayer = other.GetComponentInParent<PlayerController>() != null;

            if (_isEnemy && isPlayer)
            {
                var batch = new DamageBatch();
                batch.Deal(damageable, Damage, ElementType.Neutral);
                batch.Commit(Helpers.Combat.PlayerDamage);
                
                Detonate();
            }
            else if (!_isEnemy && !isPlayer)
            {
                var batch = new DamageBatch();
                batch.Deal(damageable, gameObject, Damage, _element);
                batch.Commit(Helpers.Combat.NormalDMG);
                
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

            if (_isEnemy)
            {
                float[] angles = { 0f, 90f, 180f, 270f };
                foreach (float angle in angles)
                {
                    Vector3 dir = Quaternion.Euler(0, angle, 0) * _fireDirection;
                    
                    var proj = Helpers.ProjFactory.Spawn<EnemyProjectile>(_normalProjectilePrefab, transform.position, Quaternion.LookRotation(dir));
                    
                    proj.Init(dir, 12f, Damage, Element, Owner); 
                }
            }

            SpawnImpactVFX(); 
            Helpers.ProjFactory.Despawn(gameObject);
        }

        public bool TryCustomReflect(Vector3 reflectDir, int bounceRunes, IStatResolver playerStats)
        {
            _isEnemy = false;
            gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");

            _fireDirection = reflectDir;
            _timer = 6f;

            float baseAtk = playerStats != null ? playerStats.AttackDamage : 10f;
            Damage = Mathf.Max(1, Mathf.RoundToInt(baseAtk * (1f + (bounceRunes * 0.5f))));

            Speed = Mathf.Max(Speed, 15f);

            return true;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            Owner = null;
        }
    }
}