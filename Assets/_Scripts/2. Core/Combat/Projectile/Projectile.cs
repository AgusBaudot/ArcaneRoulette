using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    public sealed class Projectile : BaseProjectile
    {
        public override bool IsEnemy => false;
        public override ElementType SpellElement => _cachedElement;

        [Serializable]
        public struct ElementVisual
        {
            public ElementType Element;
            public GameObject VisualGO;
        }

        [Header("Visuals")]
        [Tooltip("Map each element to its corresponding child GameObject inside the VisualPivot.")]
        [SerializeField]
        private ElementVisual[] _elementVisuals;

        private SpellInstance _source;
        private MonoBehaviour _runner;

        private int _baseDamage;
        private int _pierceCount;
        private AbilityType _abilityTypeForOnHit = AbilityType.Projectile;
        private bool _excludeBounceCastRuneForOnHitContext;
        private ElementType _cachedElement;

        // Enemies hit this flight — prevents re-triggering while passing through
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
                if (ev.VisualGO == null)
                    continue;

                _baseVisualScales[ev.VisualGO] = ev.VisualGO.transform.localScale;

                foreach (var ps in ev.VisualGO.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    _baseParticleSizes[ps] = new Vector3(
                        main.startSizeXMultiplier,
                        main.startSizeYMultiplier,
                        main.startSizeZMultiplier
                    );
                }

                foreach (var trail in ev.VisualGO.GetComponentsInChildren<TrailRenderer>(true))
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
                if (ev.VisualGO != null)
                {
                    ev.VisualGO.SetActive(ev.Element == currentElement);
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
                if (ev.VisualGO == null) continue;

                // 1. Scale the container transform
                if (_baseVisualScales.TryGetValue(ev.VisualGO, out Vector3 baseScale))
                {
                    ev.VisualGO.transform.localScale = baseScale * sizeMultiplier;
                }

                // 2. Safely scale particles WITHOUT double-scaling
                foreach (var ps in ev.VisualGO.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;

                    // Check if the Transform scaling we just did ALREADY scaled this particle system.
                    // Hierarchy always scales with parents. Local only scales if the PS is directly on the scaled object.
                    bool isAlreadyScaling = main.scalingMode == ParticleSystemScalingMode.Hierarchy ||
                                            (main.scalingMode == ParticleSystemScalingMode.Local &&
                                             ps.gameObject == ev.VisualGO);

                    if (isAlreadyScaling)
                    {
                        // The Transform scale handled it! Do NOT touch startSize, or it will double-scale.
                        continue;
                    }

                    // If we are here, the PS ignored the Transform scale. We must scale it manually.
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
                            // Fall back to uniform scaling if the 3D checkbox is not ticked
                            main.startSizeMultiplier = baseSize3D.x * sizeMultiplier;
                        }
                    }
                }

                // 3. Trails ALWAYS ignore Transforms, so they ALWAYS need manual scaling
                foreach (var trail in ev.VisualGO.GetComponentsInChildren<TrailRenderer>(true))
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
            // Resolve to the actual damageable owner so OnHit runes always receive a valid HitTarget.
            var damageable = other.GetComponentInParent<IDamageable>(true)
                             ?? other.GetComponent<IDamageable>();

            if (damageable == null)
                return;

            // Unity-friendly: IDamageable is an interface, so derive GameObject via Component.
            var damageableGo = (damageable as Component)?.gameObject ?? other.gameObject;

            // Already hit this target this flight — ignore
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

            ApplyVisualScale(1f);

            //Prevent memory leaks
            _source = null;
            _runner = null;
            _hitTargets.Clear();
        }
    }
}