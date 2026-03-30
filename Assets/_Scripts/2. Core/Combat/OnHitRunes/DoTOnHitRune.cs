using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/OnHit/DoT")]
    public class DoTOnHitRune : OnHitRuneSO
    {
        [SerializeField] private int _damagePerTick = 3;
        [SerializeField] private float _tickInterval = 0.5f;
        [SerializeField] private float _baseDuration = 2f;

        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (ctx.HitTarget == null)
                return;
            if (!ctx.HitTarget.TryGetComponent<IDamageable>(out var dmg))
                return;

            var dot = ctx.HitTarget.GetComponent<DoTComponent>()
                      ?? ctx.HitTarget.AddComponent<DoTComponent>();

            dot.Apply(dmg, _damagePerTick * stackCount, _tickInterval, _baseDuration, ctx.AttackerElement);
        }
    }
}