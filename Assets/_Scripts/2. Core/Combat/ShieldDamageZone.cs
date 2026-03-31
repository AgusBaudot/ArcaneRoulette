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
    public class ShieldDamageZone : MonoBehaviour
    {
        [SerializeField] private float _damagePerSecond = 5f;
        [SerializeField] private float _tickInterval = 0.3f;

        private float _tickTimer;
        private bool _armed;

        //Enemies currently inside the zone
        private readonly HashSet<IDamageable> _inside = new();

        public bool Active { get; set; } = false;

        private void OnEnable()
        {
            _armed = false;
            StartCoroutine(ArmNextPhysicsTick());
        }

        private System.Collections.IEnumerator ArmNextPhysicsTick()
        {
            yield return new WaitForFixedUpdate();
            _armed = true;
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

        private void Update()
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
                DamageSystem.Deal(dmg, Mathf.RoundToInt(_damagePerSecond * _tickInterval), ElementType.Neutral);
            }
        }

        private void OnDisable()
        {
            _inside.Clear();
            _tickTimer = 0f;
        }
    }
}