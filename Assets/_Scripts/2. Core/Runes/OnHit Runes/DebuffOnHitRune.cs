using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/OnHit/Debuff")]
    public sealed class DebuffOnHitRune : OnHitRuneSO
    {
        [SerializeField] private float _duration = 5f;

        [Header("Strength per Stack (index 0 = 1 stack, index 3 = 4 stacks)")]
        [Tooltip("Per-stack strength table - index 0  = 1 stack, index 4 = 5 stacks")]
        [SerializeField] private float[] _fireStrengthPerStack      = {0.10f, 0.20f, 0.30f, 0.40f, 0.50f };
        [Tooltip("Per-stack strength table - index 0  = 1 stack, index 4 = 5 stacks")]
        [SerializeField] private float[] _waterStrengthPerStack     = { 0.30f, 0.35f, 0.40f, 0.45f, 0.50f };
        [Tooltip("Per-stack strength table - index 0  = 1 stack, index 4 = 5 stacks")]
        [SerializeField] private float[] _electricStrengthPerStack = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f };
        [Tooltip("Per-stack strength table - index 0  = 1 stack, index 4 = 5 stacks")]
        [SerializeField] private float[] _earthStrengthPerStack     = { 0.2f, 0.4f, 0.6f, 0.8f, 1f };
        [Tooltip("Per-stack strength table - index 0  = 1 stack, index 4 = 5 stacks")]
        [SerializeField] private float[] _neutralStrengthPerStack   = { 0.05f, 0.10f, 0.15f, 0.20f, 0.25f };


        public override void Apply(SpellContext ctx, int stackCount)
        {
            if (ctx.HitTarget == null)
                return;

            DebuffType type = ElementToDebuffType(ctx.AttackerElement);
            float strength = SampleStrength(ctx.AttackerElement, stackCount);
            
            var debuff = ctx.HitTarget.GetComponent<DebuffComponent>()
                ?? ctx.HitTarget.AddComponent<DebuffComponent>();
            
            debuff.ApplyDebuff(type, strength, _duration);
        }

        private static DebuffType ElementToDebuffType(ElementType element)
            => element switch
            {
                ElementType.Fire => DebuffType.ATK,
                ElementType.Water => DebuffType.Speed,
                ElementType.Electric => DebuffType.AttackSpeed,
                ElementType.Earth => DebuffType.AntiHeal,
                _ => GetRandomDebuffType()
            };

        private static DebuffType GetRandomDebuffType()
        {
            var values = (DebuffType[])System.Enum.GetValues(typeof(DebuffType));
            return values[Random.Range(0, values.Length)];
        }

        private float SampleStrength(ElementType element, int stackCount)
        {
            float[] table = element switch
            {
                ElementType.Fire => _fireStrengthPerStack,
                ElementType.Water => _waterStrengthPerStack,
                ElementType.Electric => _electricStrengthPerStack,
                ElementType.Earth => _earthStrengthPerStack,
                _ => _neutralStrengthPerStack
            };
            
            //Clamp to table bounds - never throw on unexpected stack counts.
            int idx = Mathf.Clamp(stackCount - 1, 0, table.Length - 1);
            return table[idx];
        }
    }
}