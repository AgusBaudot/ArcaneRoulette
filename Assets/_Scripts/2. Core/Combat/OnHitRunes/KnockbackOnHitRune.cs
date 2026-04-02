using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/OnHit/Knockback")]
    public sealed class KnockbackOnHitRune : OnHitRuneSO
    {
        [SerializeField] private float _baseForce = 8f;

        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (ctx.HitTarget == null)
                return;
            if (!ctx.HitTarget.TryGetComponent<IKnockbackable>(out var kb))
                return;

            Vector3 dir;
            
            //Projectile and dash: push in direction of travel
            if (ctx.AttackerDirection != Vector3.zero)
                dir = ctx.AttackerDirection.normalized;

            else
            {
                //Shield, AoE secondary, any zero-direction hit: repel from contact point.
                dir = (ctx.HitTarget.transform.position - ctx.HitPosition).normalized;
                if (dir == Vector3.zero)
                {
                    Debug.Log("Defaulted to right since direction was 0");
                    dir = Vector3.right;
                }
            }
            
            kb.ApplyKnockback(dir, _baseForce * stackCount);
        }
    }
}