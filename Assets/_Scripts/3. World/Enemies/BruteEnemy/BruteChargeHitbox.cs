using UnityEngine;
using Core;
using Foundation;
using System;

namespace World
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BruteChargeHitbox : MonoBehaviour
    {
        private BoxCollider _col;
        private BruteEnemyStats _stats;
        private ElementType _element;
        private Action<float> _onInterrupted;

        private void Awake()
        {
            _col = GetComponent<BoxCollider>();
            _col.isTrigger = true;
            Deactivate();
        }

        public void Activate(BruteEnemyStats stats, ElementType element, Vector3 direction, Action<float> onInterrupted)
        {
            _stats = stats;
            _element = element;
            _onInterrupted = onInterrupted;
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            _col.enabled = true;
        }

        public void Deactivate() => _col.enabled = false;

        private void OnTriggerEnter(Collider other)
        {
            if (!_col.enabled) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                if (player.IsShielding)
                {
                    player.ForceDestroyActiveShield();
                    _onInterrupted?.Invoke(_stats.StunDurationShield);
                    return;
                }
                
                var batch = new DamageBatch();
                int damage = Mathf.RoundToInt(_stats.AttackDamage * _stats.ChargeDamagePercentage);
                batch.Deal(player.GetComponentInChildren<IDamageable>(), damage, _element);
                batch.Commit(Helpers.Combat.PlayerDamage);
                
                _onInterrupted?.Invoke(_stats.StunDurationImpact);
                return;
            }

            var bomb = other.GetComponentInParent<ElementalBomb>();
            if (bomb != null)
            {
                float totalStun = _stats.StunDurationImpact;
                
                // If elements match, the bomb triggers and the brute takes bonus stun
                if (bomb.TryDetonate(_element))
                {
                    totalStun += _stats.StunDurationBombBonus;
                }
                
                _onInterrupted?.Invoke(totalStun);
                return;
            }

            if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                _onInterrupted?.Invoke(_stats.StunDurationImpact);
            }
        }
    }
}