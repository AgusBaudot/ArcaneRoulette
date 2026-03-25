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

        // Enemies hit this flight — prevents re-triggering while passing through
        private readonly HashSet<GameObject> _hitTargets = new();

        public void Init(
            SpellInstance source,
            Vector3 direction,
            float speed,
            int baseDamage,
            float hitStopDuration,
            float cameraTrauma,
            MonoBehaviour runner)
        {
            _source = source;
            _runner = runner;
            _baseDamage = baseDamage;
            _hitStopDuration = hitStopDuration;
            _cameraTrauma = cameraTrauma;

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
            // Already hit this target this flight — ignore
            if (!_hitTargets.Add(other.gameObject)) return;

            var element = _source?.Element ?? ElementType.Neutral;
            other.GetComponent<IDamageable>().TakeDamage(_baseDamage, element);

            if (other.TryGetComponent<DamageFlash>(out var flash))
                flash.Flash();

            HitStop.Apply(_hitStopDuration);
            CameraShake.AddTrauma(_cameraTrauma);

            _source?.TriggerOnHit(transform.position, other.gameObject, _runner);

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