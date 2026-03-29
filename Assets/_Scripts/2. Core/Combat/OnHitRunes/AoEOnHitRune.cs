using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/OnHit/AoE")]
    public sealed class AoEOnHitRune : OnHitRuneSO
    {
        [SerializeField] private float _baseRadius = 3f;
        [SerializeField] private int _baseDamage = 5;
        [SerializeField] private LayerMask _enemyMask;
        
        //Explicit prototype guard - one level deep only, no recursive calls
        private static bool _isExpanding;

        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (_isExpanding)
                return;

            float radius = _baseRadius * stackCount;
            var hits = Physics.OverlapSphere(ctx.HitPosition, radius, _enemyMask);
            var primaryDamageable = ctx.HitTarget != null
                ? (ctx.HitTarget.GetComponentInParent<IDamageable>(true) ?? ctx.HitTarget.GetComponent<IDamageable>())
                : null;

            _isExpanding = true;

            foreach (var hit in hits)
            {
                var dmg = hit.GetComponentInParent<IDamageable>(true)
                          ?? hit.GetComponent<IDamageable>();
                if (dmg == null)
                    continue;

                if (primaryDamageable != null && dmg == primaryDamageable)
                    continue;
                
                DamageSystem.Deal(dmg, hit.gameObject, _baseDamage, ctx.AttackerElement);
                // dmg.TakeDamage(_baseDamage, ElementType.Neutral);
                
                if (hit.TryGetComponent<DamageFlash>(out var flash))
                    flash.Flash();
            }

            _isExpanding = false;

            CameraShake.AddTrauma(0.2f);
        }
    }
}