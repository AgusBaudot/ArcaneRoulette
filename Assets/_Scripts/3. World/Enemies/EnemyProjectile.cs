using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public sealed class EnemyProjectile : BaseProjectile, IEnemyProjectile
    {
        public override bool IsEnemy => true;
        public override ElementType SpellElement => Element;
        public int Damage => _damage;
        public ElementType Element => ElementType.Neutral;
        public GameObject Owner => _owner;
        
        [SerializeField] private int _damage  = 5;

        private GameObject _owner; //Set by EnemyAI at spawn, null for now
        
        // Called by whatever spawns this — EnemyAI, a turret, a boss pattern
        public void Init(Vector3 direction, float speed, int damage, GameObject owner = null)
        {
            _damage  = damage;
            BounceCount = 0;
            _owner = owner;
            SetVelocity(direction, speed);
            PlayParticles();
        }


        protected override void OnHitDamageable(Collider other)
        {
            var dmg = other.GetComponent<IDamageable>();
            
            var batch = new DamageBatch();
            batch.Deal(dmg, _damage, ElementType.Neutral);
            batch.Commit(Helpers.Combat.PlayerDamage);
            
            Helpers.ProjFactory.Despawn(gameObject);
        }

        protected override void OnHitWall(Collider other)
        {
            if (!TryBounce())
                Helpers.ProjFactory.Despawn(gameObject);
        }
        
        public override void OnDespawn()
        {
            base.OnDespawn();

            _owner = null;
        }
    }
}