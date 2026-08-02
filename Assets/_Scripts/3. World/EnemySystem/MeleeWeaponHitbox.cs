using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Core;

namespace World
{
    /// <summary>
    /// Lives on a child transform under the weapon socket (assign once the rig
    /// exists — placement is an authoring task, not something this component
    /// controls). MeleeAIBrain calls Configure() then Activate() at the start of
    /// each swing, and Deactivate() when that swing's window ends.
    ///
    /// While active, any IDamageable that enters is hit once per activation —
    /// same HashSet-dedup idea as Projectile.cs uses for pierce targets, so a
    /// swing can't multi-hit the player across several physics frames.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MeleeWeaponHitbox : MonoBehaviour
    {
        private BoxCollider _collider;
        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        private int _damage;
        private ElementType _element;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        public void Configure(int damage, ElementType element, Vector3 size)
        {
            _damage = damage;
            _element = element;
            _collider.size = size;
        }

        public void Activate()
        {
            _hitThisSwing.Clear();
            _collider.enabled = true;
        }

        public void Deactivate()
        {
            _collider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<IDamageable>();
            if (target == null || _hitThisSwing.Contains(target))
                return;

            _hitThisSwing.Add(target);

            var batch = new DamageBatch();
            batch.Deal(target, _damage, _element);
            batch.Commit(Helpers.Combat.PlayerDamage);
        }
    }
}