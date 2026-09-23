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
        
        [Header("Destructibles")]
        [SerializeField] private LayerMask _destructibleMask;
        
        [Header("Visuals")]
        [SerializeField] private GameObject _aoeFX;

        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (ctx.IsSecondaryHit)
                return;

            float radius = _baseRadius * stackCount;
            
            Instantiate(_aoeFX, ctx.HitPosition, Quaternion.identity);
            
            var enemyHits = Physics.OverlapSphere(ctx.HitPosition, radius, _enemyMask);
            var batch = new DamageBatch();

            foreach (var hit in enemyHits)
            {
                if (hit.gameObject == ctx.HitTarget)
                    continue;
                
                if (!hit.TryGetComponent<IDamageable>(out var dmg))
                    continue;
                
                batch.Deal(dmg, hit.gameObject, _baseDamage, ctx.AttackerElement);
                
                Vector3 pushDir = (hit.transform.position - ctx.HitPosition).normalized;
                if (pushDir == Vector3.zero)
                {
                    Debug.LogWarning("Direction is zero");
                }
                
                ctx.TriggerSecondaryHit(hit.transform.position, hit.gameObject, pushDir);
            }

            batch.Commit(Helpers.Combat.BigDMG);
            
            var destructibleHits = Physics.OverlapSphere(ctx.HitPosition, radius, _destructibleMask);

            foreach (var hit in destructibleHits)
            {
                if (hit.gameObject == ctx.HitTarget)
                    continue;
                
                var destructible = hit.GetComponent<IDestructible>();

                if (destructible == null || destructible.IsDestroyed)
                    continue;

                destructible.OnDeath(ctx.HitPosition);
                
                Vector3 pushDir = (hit.transform.position - ctx.HitPosition).normalized;
                
                ctx.TriggerSecondaryHit(hit.transform.position, hit.gameObject, pushDir);
            }
        }
    }
}