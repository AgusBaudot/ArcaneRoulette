using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Second collider on shield prefab - larger trigger that damages enemies inside.
    /// Only activate when PiercingCastRune sets AllowEnemyThrough = true.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShieldDamageZone : MonoBehaviour, IUpdatable
    {
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private float _damagePerSecond = 5f;
        [SerializeField] private float _tickInterval = 0.3f;

        //IUpdatable
        public int UpdatePriority => Foundation.UpdatePriority.Player;

        private SphereCollider _col;
        private float _tickTimer;
        private bool _armed;

        //Enemies currently inside the zone
        private readonly HashSet<IDamageable> _inside = new();

        public bool Active { get; set; }

        private void Awake()
        {
            _col = GetComponent<SphereCollider>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            _armed = false;
            StartCoroutine(ArmNextPhysicsTick());
        }

        private System.Collections.IEnumerator ArmNextPhysicsTick()
        {
            yield return new WaitForFixedUpdate();
            _armed = true;
            SeedExistingOverlaps();
        }

        private void SeedExistingOverlaps()
        {
            //Use the actual collider bounds to match what Physics sees.
            var hits = Physics.OverlapSphere(transform.TransformPoint(_col.center),
                _col.radius * transform.lossyScale.x, _enemyMask,
                QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<PlayerHurtBox>(out _))
                    continue;

                if (hit.TryGetComponent<IDamageable>(out var dmg))
                    _inside.Add(dmg);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Active || !_armed)
                return;
            // Shield zone must not damage the player owner.
            if (other.TryGetComponent<PlayerHurtBox>(out _))
                return;
            
            if (other.TryGetComponent<IDamageable>(out var dmg))
                _inside.Add(dmg);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_armed)
                return;
            if (other.TryGetComponent<PlayerHurtBox>(out _))
                return;
            if (other.TryGetComponent<IDamageable>(out var dmg))
                _inside.Remove(dmg);
        }

        public void Tick(float dt)
        {
            if (!Active || _inside.Count == 0)
                return;
            
            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f)
                return;

            _tickTimer = _tickInterval;

            foreach (var dmg in _inside)
            {
                if (dmg == null)
                    continue;

                // dmg.TakeDamage(Mathf.RoundToInt(_damagePerSecond * _tickInterval), ElementType.Neutral);
                DamageSystem.Deal(dmg, (dmg as Component)?.gameObject, Mathf.RoundToInt(_damagePerSecond * _tickInterval), ElementType.Neutral, DamageJuice.Light);
            }
        }

        private void OnDisable()
        {
            UpdateManager.Instance.Unregister(this);
            _inside.Clear();
            _tickTimer = 0f;
        }
    }
}