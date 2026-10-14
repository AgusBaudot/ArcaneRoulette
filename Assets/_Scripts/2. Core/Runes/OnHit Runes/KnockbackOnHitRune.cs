using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/OnHit/Knockback")]
    public sealed class KnockbackOnHitRune : OnHitRuneSO
    {
        [SerializeField] private float _baseForce = 8f;
        [Tooltip("Count is modified by this scaling factor (count^scaling) * base")]
        [SerializeField] private float _scaling = 0.5f;

        [Header("Visuals")]
        [SerializeField] private PooledVFX _knockbackVFX;
        [SerializeField] private float _vfxHeightOffset = 1f;

        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (ctx.HitTarget == null)
                return;
            if (!ctx.HitTarget.TryGetComponent<IKnockbackable>(out var kb))
                return;

            Vector3 dir;
            
            if (ctx.AttackerDirection != Vector3.zero)
            {
                dir = ctx.AttackerDirection.normalized;
            }
            else
            {
                dir = (ctx.HitTarget.transform.position - ctx.HitPosition).normalized;
                if (dir == Vector3.zero)
                {
                    Debug.Log("Defaulted to right since direction was 0");
                    dir = Vector3.right;
                }
            }
            
            kb.ApplyKnockback(dir, _baseForce * Mathf.Pow(stackCount, _scaling));

            if (_knockbackVFX != null)
            {
                Vector3 spawnPos = ctx.HitPosition + Vector3.up * _vfxHeightOffset;
                Helpers.ProjFactory.Spawn(_knockbackVFX, spawnPos, Quaternion.identity);
            }
        }
    }
}