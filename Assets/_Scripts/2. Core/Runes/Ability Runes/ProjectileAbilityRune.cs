using System.Collections;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Projectile")]
    public class ProjectileAbilityRune : AbilityRuneSO
    {
        [Header("Stats")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private float _windupDuration = 0.08f;
        [SerializeField] private float _cooldownDuration = 0.4f; // 1f / fireRate
        [SerializeField] private float _offset = 2f;
        [Header("Audio")]
        [SerializeField] private AudioEventSO _defaultCastSound;
        [SerializeField] private ElementalSound[] _elementalSounds;

        public override AbilityType Type => AbilityType.Projectile;
        public override bool IsHoldAbility => false;
        public override float CooldownDuration => _cooldownDuration;
        
        public override void Activate(SpellContext ctx)
        {
            AudioEventSO audio = GetCastSound(ctx.AttackerElement);
            if (audio != null)
            {
                EventBus.Publish(new AudioPlayRequest
                {
                    Event = audio,
                    WorldPosition = ctx.Runner.transform.position
                });
            }
            
            var args = new ProjectileFireArgs();
            (ctx.Source as ISpellEventSource)?.RaiseBeforeFire(args);
            ctx.Runner.StartCoroutine(WindUpThenFire(ctx, args));
        }
        
        private AudioEventSO GetCastSound(ElementType element)
        {
            foreach (var map in _elementalSounds)
            {
                if (map.Element == element)
                    return map.CastSound;
            }

            return _defaultCastSound;
        }

        private IEnumerator WindUpThenFire(SpellContext ctx, ProjectileFireArgs args)
        {
            yield return CoroutineUtils.GetWait(_windupDuration);
            Fire(ctx, args);
        }

        private void Fire(SpellContext ctx, ProjectileFireArgs args)
        {
            Ray ray = CameraUtils.GetCamera().ScreenPointToRay(Helpers.Input.MousePosition);

            if (!Physics.Raycast(ray, out var hit, 200f, Helpers.PlayerStats.ProjectileFloorMask))
            {
                Debug.LogError($"{nameof(ProjectileAbilityRune)}: Raycast failed, no floor found.");
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

            Vector3 spawnPos = ctx.Runner.transform.position + dir * _offset;
            
            float baseDamage = ctx.Stats != null ? ctx.Stats.AttackDamage : 2f;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage));
            
            var go = Helpers.ProjFactory.Spawn(_projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
            go.gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
            
            go.Init(ctx.Source as SpellInstance, dir, _projectileSpeed, finalDamage,
                ctx.Runner, AbilityType.Projectile, excludeBounceCastRuneForOnHitContext: false);
            
            go.SetPierceCount(args.PierceCount);
            go.SetBounceCount(args.BounceCount);
            
            go.SetPierceFeedback(args.PierceSounds, args.PierceHitStops);
            
            go.ApplyVisualScale(args.SizeMultiplier);

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