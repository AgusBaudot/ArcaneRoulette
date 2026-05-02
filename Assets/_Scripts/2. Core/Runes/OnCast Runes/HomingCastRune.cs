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
        // Only sets args.HomingCount. Ability runes read the count and call
        // SpawnHomingProjectiles themselves — they need this SO instance for
        // the prefab reference and designer fields, which they reach by
        // iterating Recipe.CastRunes() and breaking on the first HomingCastRune.
        public override void Subscribe(AbilityRuneSO ability, int stackCount,
            List<Action> cleanup)
        {
            switch (ability)
            {
                case ProjectileAbilityRune proj:
                {
                    Action<ProjectileFireArgs> h = args => args.HomingCount = stackCount;
                    proj.OnBeforeFire += h;
                    cleanup.Add(() => proj.OnBeforeFire -= h);
                    break;
                }
                case DashAbilityRune dash:
                {
                    Action<DashActivationArgs> h = args => args.HomingCount = stackCount;
                    dash.OnBeforeActivate += h;
                    cleanup.Add(() => dash.OnBeforeActivate -= h);
                    break;
                }
                case ShieldAbilityRune shield:
                {
                    Action<ShieldActivationArgs> h = args => args.HomingCount = stackCount;
                    shield.OnBeforeStartHold += h;
                    cleanup.Add(() => shield.OnBeforeStartHold -= h);
                    break;
                }
            }
        }

        // ── SpawnHomingProjectiles ───────────────────────────────────────────
        /// <summary>
        /// Called by all three ability runes after their activation.
        /// Damage is 30% if base damage - consistent across abilities.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="spawnPosition"></param>
        /// <param name="initialDirection"></param>
        /// <param name="speed"></param>
        /// <param name="baseDamage"></param>
        /// <param name="element"></param>
        /// <param name="runner"></param>
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

            var player = (PlayerController)runner;
            int damage = Mathf.Max(1, Mathf.RoundToInt(player.Stats.BaseDamage * _damageMultiplier));

            Vector3[] offsets = GetFormationOffsets(count, _spawnOffset);
            
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(_homingPrefab, spawnPosition + offsets[i], Quaternion.LookRotation(initialDirection));
                go.gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");

                // Scale sprite child, not root — same pattern as ProjectileAbilityRune
                if (_sizeMultiplier != 1f)
                {
                    go.transform.GetChild(0).localScale = Vector3.one * _sizeMultiplier;

                    var col = go.GetComponent<SphereCollider>();
                    if (col != null)
                        col.radius *= _sizeMultiplier / 2f;
                }

                go.Init(initialDirection, _homingSpeed, damage, element);
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