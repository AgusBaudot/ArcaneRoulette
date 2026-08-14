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

            // 1. Check for Player
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                if (player.IsShielding)
                {
                    player.ForceDestroyActiveShield();
                    _onInterrupted?.Invoke(_stats.StunDurationShield);
                    return;
                }
                
                // Normal Hit
                var batch = new DamageBatch();
                int damage = Mathf.RoundToInt(_stats.AttackDamage * _stats.ChargeDamagePercentage);
                batch.Deal(player.GetComponentInChildren<IDamageable>(), damage, _element);
                batch.Commit(Helpers.Combat.PlayerDamage);
                
                _onInterrupted?.Invoke(_stats.StunDurationImpact);
                return;
            }

            // 2. Check for Elemental Bomb (Hazard integration)
            var bomb = other.GetComponentInParent<ElementalBomb>();
            if (bomb != null)
            {
                // Architectural Bridge: The Bomb currently strictly expects a PlayerProjectile. 
                // We'll need a slight tweak on ElementalBomb to expose an internal Detonate(ElementType) 
                // method that we can call right here if elements match, granting the bonus stun:
                // if (bomb.Element == _element) { bomb.Detonate(); stun += _stats.StunDurationBombBonus; }
                _onInterrupted?.Invoke(_stats.StunDurationImpact + _stats.StunDurationBombBonus);
                return;
            }

            // 3. Environment/Solid Object (Obstacle layer)
            if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                _onInterrupted?.Invoke(_stats.StunDurationImpact);
            }
        }
    }
}