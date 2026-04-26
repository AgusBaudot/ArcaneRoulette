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
        [SerializeField] private GameObject _aoeFX;
        
        private bool _isExpanding;

        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (_isExpanding)
                return;

            float radius = _baseRadius * stackCount;
            var hits = Physics.OverlapSphere(ctx.HitPosition, radius, _enemyMask);

            Instantiate(_aoeFX, ctx.HitPosition, Quaternion.identity);

            _isExpanding = true;

            foreach (var hit in hits)
            {
                if (hit.gameObject == ctx.HitTarget)
                    continue;
                
                if (!hit.TryGetComponent<IDamageable>(out var dmg))
                    continue;

                DamageSystem.Deal(dmg, hit.gameObject, _baseDamage, ctx.AttackerElement);
                
                Vector3 pushDir = (hit.transform.position - ctx.HitPosition).normalized;
                if (pushDir == Vector3.zero)
                {
                    Debug.Log("Direction is zero");
                }
                
                //Full OnHit chain on each secondary target.
                //_isExpanding blocks AoEOnHitRune from firing again - all other
                //runes (OnCast, OnHit) run normally on secondary targets.
                ctx.TriggerSecondaryHit?.Invoke(hit.transform.position, hit.gameObject, pushDir);
            }

            _isExpanding = false;
        }
    }
}