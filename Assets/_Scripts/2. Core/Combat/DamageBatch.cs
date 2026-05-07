using Foundation;
using UnityEngine;

namespace Core
{
    public struct DamageBatch
    {
        private bool _anyHit;

        /// <summary>
        /// Resolves damage against one target.
        /// Triggers a local DamageFlash on the target GO if damage landed.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetObject"></param>
        /// <param name="baseDamage"></param>
        /// <param name="attackerElement"></param>
        /// <returns></returns>
        public DamageResult Deal(
            IDamageable target,
            GameObject targetObject,
            int baseDamage,
            ElementType attackerElement)
        {
            DamageResult result = DamageSystem.Deal(target, targetObject, baseDamage, attackerElement);

            if (result.DidDamage)
            {
                _anyHit = true;
                if (target != null && targetObject.TryGetComponent(out DamageFlash flash))
                    flash.Flash();
            }
            
            return result;
        }

        /// <summary>
        /// overload for callers without an explicit GO reference.
        /// Resolves it form the IDamageable Mono
        /// </summary>
        /// <returns></returns>
        public DamageResult Deal(IDamageable target, int baseDamage, ElementType attackerElement)
        {
            var go = (target as MonoBehaviour)?.gameObject;
            return Deal(target, go, baseDamage, attackerElement);
        }

        /// <summary>
        /// Fires global juice once if any Deal() call in this batch landed damage.
        /// Safe to call unconditionally - skips silently if nothing was hit.
        /// </summary>
        /// <param name="juice"></param>
        public void Commit(DamageJuice juice)
        {
            if (!_anyHit)
                return;

            GameFeelSystem.PlayJuice(juice);
        }

        /// <summary>
        /// Commit with default juice.
        /// </summary>
        public void Commit() => Commit(Helpers.Combat.NormalDMG);
    }
}