using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        private SpellInstance _source;
        private MonoBehaviour _runner;
        private Rigidbody _rb;

        private int _baseDamage;
        private float _hitStopDuration;
        private float _cameraTrauma;
        private float _knockbackForce;
        
        //Set by PiercingCastRune in Phase 3 via vtx.Modifiers.PierceCount
        public int PierceCount { get; set; } = 0;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth at any framerate
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotation;
        }

        //Called by ProjectileAbilityRune.Fire() immediately after Instantiate
        public void Init(SpellInstance source, Vector3 direction, float speed, int baseDamage, float hitStopDuration,
            float cameraTrauma, float knockbackForce, MonoBehaviour runner)
        {
            _source = source;
            _runner = runner;
            _baseDamage = baseDamage;
            _hitStopDuration = hitStopDuration;
            _cameraTrauma = cameraTrauma;
            _knockbackForce = knockbackForce;

            _rb.velocity = direction.normalized * speed;
            transform.forward = direction.normalized;

            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                ps.Clear();
                ps.Play();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            //Base damage - dealt directly until DamageSystem is wired in Phase 5.
            //Element type passed here; resistance table activates in Phase 5.
            //PROTOTYPE: replace with DamageSystem.Deal() call post-Phase 5.
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                var element = _source?.Element ?? ElementType.Neutral;
                damageable.TakeDamage(_baseDamage, element);
                
                if (other.TryGetComponent<IKnockbackable>(out var kb))
                    kb.ApplyKnockback(_rb.velocity.normalized, _knockbackForce);
                
                if (other.TryGetComponent<DamageFlash>(out var flash))
                    flash.Flash();
                
                HitStop.Apply(_hitStopDuration);
                CameraShake.AddTrauma(_cameraTrauma);
            }
            
            //OnHit rune effects - empty list in Phase 1, populated in Phase 4.
            _source?.TriggerOnHit(transform.position, other.gameObject, _runner);

            if (PierceCount <= 0)
                Destroy(gameObject);
            else
                PierceCount--;
        }
    }
}