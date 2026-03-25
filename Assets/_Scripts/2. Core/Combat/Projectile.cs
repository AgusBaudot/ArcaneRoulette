using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    public sealed class Projectile : BaseProjectile
    {
        public override bool IsEnemy => false;
        
        private SpellInstance _source;
        private MonoBehaviour _runner;

        private int _baseDamage;
        private int _pierceCount;
        private float _hitStopDuration;
        private float _cameraTrauma;
        private AbilityType _abilityTypeForOnHit = AbilityType.Projectile;
        private bool _excludeBounceCastRuneForOnHitContext;

        // Enemies hit this flight — prevents re-triggering while passing through
        private readonly HashSet<GameObject> _hitTargets = new();

        public void Init(
            SpellInstance source,
            Vector3 direction,
            float speed,
            int baseDamage,
            float hitStopDuration,
            float cameraTrauma,
            MonoBehaviour runner,
            AbilityType abilityTypeForOnHit,
            bool excludeBounceCastRuneForOnHitContext)
        {
            _source = source;
            _runner = runner;
            _baseDamage = baseDamage;
            _hitStopDuration = hitStopDuration;
            _cameraTrauma = cameraTrauma;
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

            var element = _source?.Element ?? ElementType.Neutral;
            damageable.TakeDamage(_baseDamage, element);

            if (other.TryGetComponent<DamageFlash>(out var flash))
                flash.Flash();

            HitStop.Apply(_hitStopDuration);
            CameraShake.AddTrauma(_cameraTrauma);

            _source?.TriggerOnHit(
                transform.position,
                damageableGo,
                _runner,
                _abilityTypeForOnHit,
                _excludeBounceCastRuneForOnHitContext);

            if (_pierceCount <= 0)
            {
                Destroy(gameObject);
                return;
            }

            _pierceCount--;
            // Projectile continues — _hitTargets prevents re-hitting this enemy
        }

        protected override void OnHitWall(Collider other)
        {
            if (!TryBounce())
                Destroy(gameObject);
            // On bounce, _hitTargets is intentionally NOT cleared —
            // a bounced projectile can't re-hit an enemy it already pierced through.
            // Designers can revisit this if needed.
        }
    }
}