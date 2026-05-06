using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    public sealed class Projectile : BaseProjectile
    {
        public override bool IsEnemy => false;
        public override ElementType SpellElement => _source?.SpellElement ?? ElementType.Neutral;

        private SpellInstance _source;
        private MonoBehaviour _runner;

        private int _baseDamage;
        private int _pierceCount;
        private AbilityType _abilityTypeForOnHit = AbilityType.Projectile;
        private bool _excludeBounceCastRuneForOnHitContext;

        // Enemies hit this flight — prevents re-triggering while passing through
        private readonly HashSet<GameObject> _hitTargets = new();

        public void Init(
            SpellInstance source,
            Vector3 direction,
            float speed,
            int baseDamage,
            MonoBehaviour runner,
            AbilityType abilityTypeForOnHit,
            bool excludeBounceCastRuneForOnHitContext)
        {
            _source = source;
            _runner = runner;
            _baseDamage = baseDamage;
            _abilityTypeForOnHit = abilityTypeForOnHit;
            _excludeBounceCastRuneForOnHitContext = excludeBounceCastRuneForOnHitContext;

            BounceCount = 0;
            _pierceCount = 0;
            _hitTargets.Clear();

            SetVelocity(direction, speed);
            PlayParticles();
        }

        public void SetPierceCount(int count) => _pierceCount = count;
        public void SetBounceCount(int count) => BounceCount = count;

        protected override void OnHitDamageable(Collider other)
        {
            // Resolve to the actual damageable owner so OnHit runes always receive a valid HitTarget.
            var damageable = other.GetComponentInParent<IDamageable>(true)
                              ?? other.GetComponent<IDamageable>();
            
            if (damageable == null)
                return;

            // Unity-friendly: IDamageable is an interface, so derive GameObject via Component.
            var damageableGo = (damageable as Component)?.gameObject ?? other.gameObject;

            // Already hit this target this flight — ignore
            if (!_hitTargets.Add(damageableGo)) return;

            // var element = _source?.Element ?? ElementType.Neutral;
            // damageable.TakeDamage(_baseDamage, element);
            DamageSystem.Deal(damageable, damageableGo, _baseDamage, _source?.SpellElement ?? ElementType.Neutral);

            _source?.TriggerOnHit(
                transform.position,
                damageableGo,
                _runner,
                _abilityTypeForOnHit,
                _excludeBounceCastRuneForOnHitContext,
                Rb.velocity.normalized);

            if (_pierceCount <= 0)
            {
                Helpers.ProjFactory.Despawn(gameObject);
                return;
            }

            _pierceCount--;
            // Projectile continues — _hitTargets prevents re-hitting this enemy
        }

        protected override void OnHitWall(Collider other)
        {
            if (!TryBounce())
                Helpers.ProjFactory.Despawn(gameObject);
            // On bounce, _hitTargets is intentionally NOT cleared —
            // a bounced projectile can't re-hit an enemy it already pierced through.
        }

        public override void OnDespawn()
        {
            base.OnDespawn(); //Halts physics.
            
            //Prevent memory leaks
            _source = null;
            _runner = null;
            _hitTargets.Clear();
        }
    }
}