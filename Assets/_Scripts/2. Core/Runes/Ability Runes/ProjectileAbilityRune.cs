using System;
using System.Collections;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Projectile")]
    public class ProjectileAbilityRune : AbilityRuneSO
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private int _baseDamage = 10;
        [SerializeField] private float _windupDuration = 0.08f;
        [SerializeField] private float _cooldownDuration = 0.4f; // 1f / fireRate

        public override AbilityType Type => AbilityType.Projectile;
        public override bool IsHoldAbility => false;
        public override float CooldownDuration => _cooldownDuration;
        
        // ── Hook event ───────────────────────────────────────────────────────
        // Cast runes subscribe at SpellInstance construction.
        // Invoked once per Activate with a fresh ProjectileFireArgs.
        // NOTE: Shared SO state — deferred concern identical to mutable SO
        //   fields on Dash/Shield. Safe for single-player prototype.
        //   Fix: move event to per-instance state bag when multiplayer lands.
        public event Action<ProjectileFireArgs> OnBeforeFire;
        
        public override void Activate(SpellContext ctx)
        {
            var args = new ProjectileFireArgs(); //Default values = baseline.
            OnBeforeFire?.Invoke(args); //Cast runes write into args.
            ctx.Runner.StartCoroutine(WindUpThenFire(ctx, args));
        }

        private IEnumerator WindUpThenFire(SpellContext ctx, ProjectileFireArgs args)
        {
            yield return CoroutineUtils.GetWait(_windupDuration);
            Fire(ctx, args);
        }

        private void Fire(SpellContext ctx, ProjectileFireArgs args)
        {
            Ray ray = CameraUtils.GetCamera().ScreenPointToRay(Helpers.Input.MousePosition);

            if (!Physics.Raycast(ray, out var hit, 200f, LayerMask.GetMask("Floor")))
            {
                Debug.LogError($"{nameof(ProjectileAbilityRune)}: Raycast Failed");
                return;
            }

            Vector3 dir = hit.point - ctx.Runner.transform.position;
            dir.y = 0f;
            dir.Normalize();
            
            if (dir == Vector3.zero)
            {
                Debug.LogError($"{nameof(ProjectileAbilityRune)}: Direction is zero");
                return;
            }

            Vector3 spawnPos = ctx.Runner.transform.position + dir;
            
            var go = Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
            go.gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
            go.Init(ctx.Source as SpellInstance, dir, _projectileSpeed, _baseDamage,
                ctx.Runner, AbilityType.Projectile, excludeBounceCastRuneForOnHitContext: false);
            go.SetPierceCount(args.PierceCount);
            go.SetBounceCount(args.BounceCount);
            
            if (args.SizeMultiplier != 1f)
            {
                go.transform.GetChild(0).localScale = Vector3.one * args.SizeMultiplier;
                
                var col = go.GetComponent<SphereCollider>();
                if (col != null)
                    col.radius *= args.SizeMultiplier / 2;
            }

            if (args.HomingCount > 0)
                SpawnHomingProjectiles(ctx, dir, args.HomingCount);
        }

        private void SpawnHomingProjectiles(SpellContext ctx, Vector3 dir, int count)
        {
            if (ctx.Source is not SpellInstance si) return;

            foreach (var castRune in si.Recipe.CastRunes())
            {
                if (castRune is not HomingCastRune homing) 
                    continue;

                homing.SpawnHomingProjectiles(
                    count,
                    ctx.Runner.transform.position,
                    dir,
                    ctx.Source.SpellElement,
                    ctx.Runner);
                
                break;
            }
        }

        //Hold lifecycle - never called, Projectile is not a hold ability
        public override void StartHold(SpellContext ctx)
        {
        }

        public override void StopHold(SpellContext ctx)
        {
        }

        public override void HoldTick(SpellContext ctx, float deltaTime)
        {
        }
    }
}