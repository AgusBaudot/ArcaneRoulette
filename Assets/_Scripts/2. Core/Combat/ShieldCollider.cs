using System;
using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShieldCollider : MonoBehaviour
    {
        [SerializeField] private Projectile _reflectedProjectilePrefab;
        [SerializeField] private float _reflectHitStop   = 0.05f;
        [SerializeField] private float _reflectTrauma    = 0.1f;

        public bool ReflectsProjectiles { get; set; }

        public event Action<Vector3, GameObject> OnProjectileAbsorbed;
        // Fired when shield bounce reflects an incoming enemy projectile.
        // Designers: in bounce mode, shield should not trigger the full OnHit rune set here.
        public event Action<Vector3, GameObject> OnProjectileReflected;
        public event Action<Vector3, GameObject> OnEnemyBodyContact;

        private SpellInstance  _boundInstance;
        private MonoBehaviour  _runner;
        // NOTE: Shield should ignore player-owner contacts via IsOwnedByRunner().

        private bool IsOwnedByRunner(Collider other)
        {
            if (_runner == null) return false;
            var runnerTf = _runner.transform;
            if (other == null || other.transform == null) return false;
            // Shield spawns on/near the player; ignore any contacts with player's own colliders.
            return other.transform == runnerTf
                   || other.transform.IsChildOf(runnerTf)
                   || other.transform.root == runnerTf.root;
        }

        // Called once by StartHoldWithInstance after instantiation
        public void Bind(SpellInstance source, MonoBehaviour runner)
        {
            _boundInstance = source;
            _runner        = runner;
        }

        private void OnCollisionEnter(Collision other)
            =>  HandleContact(other.collider, other.contacts[0].point);

        private void OnTriggerEnter(Collider other)
            => HandleContact(other, other.transform.position);

        private void HandleContact(Collider other, Vector3 contactPoint)
        {
            if (IsOwnedByRunner(other))
                return;

            if (!other.TryGetComponent<IProjectile>(out var projectile))
            {
                // Use root damageable so colliders on child objects still trigger runes.
                var dmgResolved = other.GetComponentInParent<IDamageable>(true)
                                  ?? other.GetComponent<IDamageable>();
                if (dmgResolved is { } dmg)
                {
                    var dmgGo = (dmg as Component)?.gameObject ?? other.gameObject;
                    OnEnemyBodyContact?.Invoke(contactPoint, dmgGo);
                }
                return;
            }

            if (!projectile.IsEnemy) return;

            var incomingProjectile = other.gameObject;
            var incomingPos        = incomingProjectile.transform.position;

            if (ReflectsProjectiles
                && _boundInstance != null
                && _reflectedProjectilePrefab != null
                && other.TryGetComponent<IEnemyProjectile>(out var enemy))
            {
                var reflectedDir = -projectile.Rb.velocity.normalized;
                var speed        = projectile.Rb.velocity.magnitude;

                Destroy(incomingProjectile);

                var friendly = Instantiate(
                    _reflectedProjectilePrefab,
                    incomingPos,
                    Quaternion.identity);

                friendly.Init(
                    _boundInstance,
                    reflectedDir,
                    speed,
                    enemy.Damage,
                    _reflectHitStop,
                    _reflectTrauma,
                    _runner,
                    AbilityType.Projectile,
                    excludeBounceCastRuneForOnHitContext: true);

                // Reflected projectile inherits all Cast+OnHit runes except the bounce cast rune.
                // Cast modifiers are re-derived using AbilityType.Projectile and BounceCastRune excluded.
                var mods = _boundInstance.BuildProjectileCastModifiersForReflection(_runner);
                friendly.SetPierceCount(mods.PierceCount);
                friendly.SetBounceCount(mods.BounceCount); // should be 0 due to bounce excluded
                friendly.transform.localScale = Vector3.one * mods.SizeMultiplier;

                var col = friendly.GetComponent<SphereCollider>();
                if (col)
                    col.radius *= mods.SizeMultiplier;

                OnProjectileReflected?.Invoke(contactPoint, incomingProjectile);
            }
            else
            {
                Destroy(incomingProjectile);
                OnProjectileAbsorbed?.Invoke(contactPoint, incomingProjectile);
            }
        }
    }
}