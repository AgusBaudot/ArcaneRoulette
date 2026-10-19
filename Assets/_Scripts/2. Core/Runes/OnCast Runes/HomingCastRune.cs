using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Cast/Homing")]
    public sealed class HomingCastRune : CastRuneSO
    {
        [SerializeField] private HomingProjectile _homingPrefab;
        [SerializeField] private float _homingSpeed = 12f;
        [SerializeField] private float _sizeMultiplier = 0.5f;     // Smaller than main projectile per spec
        [SerializeField] private float _damageMultiplier = 0.3f;   // 30% of spell damage per spec

        [Tooltip("Radius of the spawn formation around the player.")] [SerializeField]
        private float _spawnOffset = 0.5f;

        // ── Subscribe ────────────────────────────────────────────────────────
        public override void Subscribe(AbilityRuneSO ability, ISpellEventSource source, int stackCount,
            List<Action> cleanup)
        {
            switch (ability)
            {
                case ProjectileAbilityRune:
                {
                    Action<ProjectileFireArgs> h = args => args.HomingCount = stackCount;
                    source.OnBeforeFire += h;
                    cleanup.Add(() => source.OnBeforeFire -= h);
                    break;
                }
                case DashAbilityRune:
                {
                    Action<DashActivationArgs> h = args => args.HomingCount = stackCount;
                    source.OnBeforeActivate += h;
                    cleanup.Add(() => source.OnBeforeActivate -= h);
                    break;
                }
                case ShieldAbilityRune:
                {
                    Action<ShieldActivationArgs> h = args => args.HomingCount = stackCount;
                    source.OnBeforeStartHold += h;
                    cleanup.Add(() => source.OnBeforeStartHold -= h);
                    break;
                }
            }
        }

        // ── SpawnHomingProjectiles ───────────────────────────────────────────
        public void SpawnHomingProjectiles(
            int count,
            Vector3 spawnPosition,
            Vector3 initialDirection,
            ElementType element,
            MonoBehaviour runner)
        {
            if (_homingPrefab == null)
            {
                Debug.LogError($"{nameof(HomingCastRune)}: _homingPrefab is not assigned.");
                return;
            }

            var stats = runner.GetComponent<IStatResolver>();
            int damage = Mathf.Max(1, Mathf.RoundToInt(stats.AttackDamage * _damageMultiplier));

            Vector3[] offsets = GetFormationOffsets(count, _spawnOffset);
            
            for (int i = 0; i < count; i++)
            {
                var go = Helpers.ProjFactory.Spawn(_homingPrefab, spawnPosition + offsets[i], Quaternion.LookRotation(initialDirection));
                go.gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");

                // Init must run first to assign and activate the correct elemental visual
                go.Init(initialDirection, _homingSpeed, damage, element);

                // Safely apply scaling using the projectile's internal cached references
                if (_sizeMultiplier != 1f)
                {
                    go.ApplyVisualScale(_sizeMultiplier);
                }
            }
        }

        private Vector3[] GetFormationOffsets(int count, float radius)
        {
            var offsets = new Vector3[count];

            float startAngle = count switch
            {
                1 => 0f,
                2 => Mathf.PI / 2f,
                3 => 0f,
                4 => Mathf.PI / 4f,
                _ => 0f
            };

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + Mathf.PI * 2f * i / count;
                
                //sin -> x (right), cox -> z (forward).
                offsets[i] = new Vector3(
                    Mathf.Sin(angle),
                    0f,
                    Mathf.Cos(angle)) * radius;
            }
            
            return offsets;
        }
    }
}