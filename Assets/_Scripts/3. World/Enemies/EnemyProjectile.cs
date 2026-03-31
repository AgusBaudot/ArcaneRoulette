using UnityEngine;
using Foundation;
using Core;

namespace World
{
    public sealed class EnemyProjectile : BaseProjectile, IEnemyProjectile
    {
        public override bool IsEnemy => true;
        public int Damage => _damage;
        public ElementType Element => _element;
        public GameObject Owner => _owner;
        
        [SerializeField] private int         _damage  = 5;
        [SerializeField] private ElementType _element = ElementType.Neutral;

        private GameObject _owner; //Set by EnemyAI at spawn, null for now
        
        // Called by whatever spawns this — EnemyAI, a turret, a boss pattern
        public void Init(Vector3 direction, float speed, int damage, ElementType element, GameObject owner = null)
        {
            _damage  = damage;
            _element = element;
            BounceCount = 0;
            _owner = owner;
            SetVelocity(direction, speed);
            PlayParticles();
        }


        protected override void OnHitDamageable(Collider other)
        {
            other.GetComponent<IDamageable>().TakeDamage(_damage, _element);
            Destroy(gameObject);
        }

        protected override void OnHitWall(Collider other)
        {
            if (!TryBounce())
                Destroy(gameObject);
        }
    }
}