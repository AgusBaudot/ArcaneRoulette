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

            _isExpanding = true;

            foreach (var hit in hits)
            {
                if (hit.gameObject == ctx.HitTarget)
                    continue;
                
                if (!hit.TryGetComponent<IDamageable>(out var dmg))
                    continue;

                DamageSystem.Deal(dmg, hit.gameObject, _baseDamage, ctx.AttackerElement);
                
                if (hit.TryGetComponent<DamageFlash>(out var flash))
                    flash.Flash();
                
                //Dash/Shield: keep dash direction for secondary hits
                //Projectile: repel form AoE center (zero - repel from HitPosition).
                Vector3 secondaryDir = ctx.AbilityType == AbilityType.Projectile
                    ? Vector3.zero
                    : ctx.AttackerDirection;
                
                //Full OnHit chain on each secondary target.
                //_isExpanding blocks AoEOnHitRune from firing again - all other
                //runes (OnCast, OnHit) run normally on secondary targets.
                ctx.TriggerSecondaryHit?.Invoke(hit.transform.position, hit.gameObject, secondaryDir);
            }

            _isExpanding = false;

            CameraShake.AddTrauma(0.2f);
        }
    }
}