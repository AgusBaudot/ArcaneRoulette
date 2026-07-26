using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    public sealed class Projectile : BaseProjectile
    {
        public override bool IsEnemy => false;
        public override ElementType SpellElement => _cachedElement;

        [Header("Visuals")]
        [Tooltip("Map each element to its corresponding child GameObject inside the VisualPivot.")]
        [SerializeField]
        private ElementalGameObject[] _elementVisuals;

        private SpellInstance _source;
        private MonoBehaviour _runner;

        private int _baseDamage;
        private int _pierceCount;
        private AbilityType _abilityTypeForOnHit = AbilityType.Projectile;
        private bool _excludeBounceCastRuneForOnHitContext;
        private ElementType _cachedElement;

        private readonly HashSet<GameObject> _hitTargets = new();

        private readonly Dictionary<TrailRenderer, float> _baseTrailWidths = new();
        private readonly Dictionary<GameObject, Vector3> _baseVisualScales = new();
        private readonly Dictionary<ParticleSystem, Vector3> _baseParticleSizes = new();
        private SphereCollider _collider;
        private float _baseColliderRadius;

        protected override void Awake()
        {
            base.Awake();

            _collider = GetComponent<SphereCollider>();
            if (_collider != null)
            {
                _baseColliderRadius = _collider.radius;
            }

            if (_elementVisuals == null)
                return;

            foreach (var ev in _elementVisuals)
            {
                if (ev.Reference == null)
                    continue;

                _baseVisualScales[ev.Reference] = ev.Reference.transform.localScale;

                foreach (var ps in ev.Reference.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    _baseParticleSizes[ps] = new Vector3(
                        main.startSizeXMultiplier,
                        main.startSizeYMultiplier,
                        main.startSizeZMultiplier
                    );
                }

                foreach (var trail in ev.Reference.GetComponentsInChildren<TrailRenderer>(true))
                {
                    _baseTrailWidths[trail] = trail.widthMultiplier;
                }
            }
        }

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
            _cachedElement = _source?.SpellElement ?? ElementType.Neutral;

            BounceCount = 0;
            _pierceCount = 0;
            _hitTargets.Clear();

            UpdateActiveVisual(SpellElement);

            SetVelocity(direction, speed);
            PlayParticles();
        }

        private void UpdateActiveVisual(ElementType currentElement)
        {
            if (_elementVisuals == null)
                return;

            foreach (var ev in _elementVisuals)
            {
                if (ev.Reference != null)
                {
                    ev.Reference.SetActive(ev.Element == currentElement);
                }
            }
        }

        public void ApplyVisualScale(float sizeMultiplier)
        {
            if (_collider != null)
            {
                _collider.radius = _baseColliderRadius * sizeMultiplier;
            }

            if (_elementVisuals == null) return;

            foreach (var ev in _elementVisuals)
            {
                if (ev.Reference == null) continue;

                if (_baseVisualScales.TryGetValue(ev.Reference, out Vector3 baseScale))
                {
                    ev.Reference.transform.localScale = baseScale * sizeMultiplier;
                }

                foreach (var ps in ev.Reference.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;

                    bool isAlreadyScaling = main.scalingMode == ParticleSystemScalingMode.Hierarchy ||
                                            (main.scalingMode == ParticleSystemScalingMode.Local &&
                                             ps.gameObject == ev.Reference);

                    if (isAlreadyScaling)
                    {
                        continue;
                    }

                    if (_baseParticleSizes.TryGetValue(ps, out Vector3 baseSize3D))
                    {
                        if (main.startSize3D)
                        {
                            main.startSizeXMultiplier = baseSize3D.x * sizeMultiplier;
                            main.startSizeYMultiplier = baseSize3D.y * sizeMultiplier;
                            main.startSizeZMultiplier = baseSize3D.z * sizeMultiplier;
                        }
                        else
                        {
                            main.startSizeMultiplier = baseSize3D.x * sizeMultiplier;
                        }
                    }
                }

                foreach (var trail in ev.Reference.GetComponentsInChildren<TrailRenderer>(true))
                {
                    if (_baseTrailWidths.TryGetValue(trail, out float baseWidth))
                    {
                        trail.widthMultiplier = baseWidth * sizeMultiplier;
                    }
                }
            }
        }

        public void SetPierceCount(int count) => _pierceCount = count;
        public void SetBounceCount(int count) => BounceCount = count;

        protected override void OnHitDamageable(Collider other)
        {
            var damageable = other.GetComponentInParent<IDamageable>(true)
                             ?? other.GetComponent<IDamageable>();

            if (damageable == null)
                return;

            var damageableGo = (damageable as Component)?.gameObject ?? other.gameObject;

            if (!_hitTargets.Add(damageableGo)) return;

            var batch = new DamageBatch();
            batch.Deal(damageable, damageableGo, _baseDamage, _source.SpellElement);
            batch.Commit(Helpers.Combat.NormalDMG);


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
        }

        protected override void OnHitWall(Collider other)
        {
            _source?.TriggerOnHit(
                transform.position,
                other.gameObject,
                _runner,
                _abilityTypeForOnHit,
                _excludeBounceCastRuneForOnHitContext,
                Rb.velocity.normalized);

            other.GetComponentInParent<IDestructible>()?.OnDeath(transform.position);

            if (!TryBounce())
            {
                Helpers.ProjFactory.Despawn(gameObject);
            }
        }

        public override void OnDespawn()
        {
            base.OnDespawn(); //Halts physics.

            ApplyVisualScale(1f);

            _source = null;
            _runner = null;
            _hitTargets.Clear();
        }
    }
}