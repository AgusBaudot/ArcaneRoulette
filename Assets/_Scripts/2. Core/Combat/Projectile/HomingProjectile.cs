using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class HomingProjectile : BaseProjectile, IFixedUpdatable
    {
        public int FixedUpdatePriority => UpdatePriority.Projectile;
        public override bool IsEnemy => false;
        public override ElementType SpellElement => _element;

        [SerializeField] private float _seekRadius = 20f;
        [SerializeField] private float _turnSpeed = 6f; //Degrees interpolation per second.

        [Header("Visuals")]
        [Tooltip("Map each element to its corresponding child GameObject inside the VisualPivot.")]
        [SerializeField]
        private ElementalGameObject[] _elementVisuals;

        private int _damage;
        private ElementType _element;
        private float _speed;
        private Transform _currentTarget;
        
        private SphereCollider _collider;
        private float _baseColliderRadius;
        
        private readonly Dictionary<TrailRenderer, float> _baseTrailWidths = new();
        private readonly Dictionary<GameObject, Vector3> _baseVisualScales = new();
        private readonly Dictionary<ParticleSystem, Vector3> _baseParticleSizes = new();

        //Layer mask cached once - avoids string lookup every FixedUpdate
        private static int _enemyLayerMask = -1;

        protected override void Awake()
        {
            base.Awake();
            
            _collider = GetComponent<SphereCollider>();
            if (_collider != null)
                _baseColliderRadius = _collider.radius;

            if (_elementVisuals == null) return;

            foreach (var ev in _elementVisuals)
            {
                if (ev.Reference == null) continue;

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

        private void OnEnable() => UpdateManager.Instance.Register(this);
        private void OnDisable() => UpdateManager.Instance?.Unregister(this);

        public void Init(Vector3 initialDirection, float speed, int damage, ElementType element)
        {
            _damage = damage;
            _element = element;
            _speed = speed;
            
            if (_enemyLayerMask == -1)
                _enemyLayerMask = LayerMask.GetMask("Enemy");

            UpdateActiveVisual(_element);

            SetVelocity(initialDirection, speed);
            PlayParticles();
        }
        
        private void UpdateActiveVisual(ElementType currentElement)
        {
            if (_elementVisuals == null) return;

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

                    if (isAlreadyScaling) continue;

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

        public void FixedTick(float deltaTime)
        {
            AcquireOrRevalidateTarget();

            if (_currentTarget == null) return;
            
            SteerToward(_currentTarget.position);
        }

        private void AcquireOrRevalidateTarget()
        {
            if (_currentTarget != null &&
                _currentTarget.gameObject.activeInHierarchy &&
                _currentTarget.GetComponent<IDamageable>() != null)
                return;

            _currentTarget = null;
            FindNearestEnemy();
        }

        private void FindNearestEnemy()
        {
            var hits = Physics.OverlapSphere(transform.position, _seekRadius, _enemyLayerMask);

            float bestDist = float.MaxValue;
            Transform best = null;

            foreach (var hit in hits)
            {
                if (hit.GetComponentInParent<IDamageable>() == null)
                    continue;

                float dist = (hit.transform.position - transform.position).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = hit.transform;
                }
            }

            _currentTarget = best;
        }

        private void SteerToward(Vector3 targetPosition)
        {
            Vector3 desiredDir = (targetPosition - transform.position);
            desiredDir.y = 0f;
            if (desiredDir == Vector3.zero) return;
            desiredDir.Normalize();

            Vector3 steered = Vector3.Slerp(
                Rb.velocity.normalized,
                desiredDir,
                _turnSpeed * Time.fixedDeltaTime);

            steered.y = 0f;
            steered.Normalize();

            Rb.velocity = steered * _speed;
            transform.forward = steered;
        }

        protected override void OnHitDamageable(Collider other)
        {
            var damageable = other.GetComponentInParent<IDamageable>(true)
                             ?? other.GetComponent<IDamageable>();
            if (damageable == null) return;

            var batch = new DamageBatch();
            batch.Deal(damageable, other.gameObject, _damage, _element);
            batch.Commit(Helpers.Combat.SmallDMG);
            
            Helpers.ProjFactory.Despawn(gameObject);
        }

        protected override void OnHitWall(Collider other)
        {
            Helpers.ProjFactory.Despawn(gameObject);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();

            _currentTarget = null;
            
            // Clean pool reset — wipe any active scaling before reuse
            ApplyVisualScale(1f);
        }
    }
}