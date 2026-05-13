using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Dash")]
    public sealed class DashAbilityRune : AbilityRuneSO
    {
        [SerializeField] private float _dashSpeed = 20f;
        [SerializeField] private float _baseDashDuration = 0.2f;
        [SerializeField] private float _cooldownDuration = 0.8f;
        [SerializeField] private int _baseDamage = 8;
        [SerializeField] private float _dashHitRadius = 0.8f;
        [SerializeField] private Projectile _reflectedProjectilePrefab;

        public override AbilityType Type => AbilityType.Dash;
        public override bool IsHoldAbility => false;
        public override float CooldownDuration => _cooldownDuration;

        public override void Activate(SpellContext ctx)
        {
            var args = new DashActivationArgs();
            (ctx.Source as ISpellEventSource)?.RaiseBeforeActivate(args);
            ctx.Runner.StartCoroutine(DashRoutine(ctx, (PlayerController)ctx.Runner,
                _baseDashDuration * args.DurationMultiplier, args));
        }

        private IEnumerator DashRoutine(SpellContext ctx, PlayerController player, float duration, DashActivationArgs args)
        {
            // Determine direction — last input direction, fallback to facing
            Vector2 raw = player.LastInputDirection;
            Vector3 dir = new Vector3(raw.x, 0f, raw.y).normalized;
            
            if (dir == Vector3.zero) 
                dir = player.transform.forward;

            // Invincibility — distinct from IFrames per locked decisions
            player.SetCanMove(false);
            player.Hurtbox.SetActive(false);
            
            var hitEnemies = new HashSet<GameObject>();

            //Spawn homing projectiles before dash begins
            if (args.HomingCount > 0)
                SpawnHomingFromDash(ctx, dir, args.HomingCount);
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                //Enemy collision - OnHit per enemy touched
                var enemies = Physics.OverlapSphere(
                    player.transform.position, _dashHitRadius, player.Stats.EnemyLayerMask);

                var batch = new DamageBatch();

                foreach (var hit in enemies)
                {
                    if (!hitEnemies.Add(hit.gameObject))
                        continue;
                    
                    if (!hit.TryGetComponent<IDamageable>(out var dmg)) 
                        continue;

                    // Damage only if PiercingCastRune is slotted
                    if (args.DamagesOnDash)
                    {
                        batch.Deal(dmg, hit.gameObject, _baseDamage, ctx.Source.SpellElement);
                    }

                    // OnHit runes always fire per enemy touched regardless of damage
                    ctx.Source.TriggerOnHit(hit.transform.position, hit.gameObject, ctx.Runner,
                        AbilityType.Dash, false, dir);
                }
                
                batch.Commit(Helpers.Combat.NormalDMG);

                // ── Enemy projectile reflection (Bounce rune) ────────────────────
                if (args.ReflectCount > 0 && _reflectedProjectilePrefab != null)
                {
                    ReflectNearbyProjectiles(player, dir, ctx, ctx.Source as SpellInstance, args);
                }
                
                player.Rigidbody.velocity = dir * _dashSpeed;
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            player.Rigidbody.velocity = Vector3.zero;
            player.SetCanMove(true);
            player.Hurtbox.SetActive(true);
        }

        private void SpawnHomingFromDash(SpellContext ctx, Vector3 dir, int count)
        {
            if (ctx.Source is not SpellInstance si)
                return;

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

        private void ReflectNearbyProjectiles(PlayerController player, Vector3 dashDir, SpellContext ctx,
            SpellInstance source, DashActivationArgs args)
        {
            //Detect enemy projectiles in dash radius
            var cols = Physics.OverlapSphere(player.transform.position, _dashHitRadius * 2f);

            foreach (var col in cols)
            {
                if (!col.TryGetComponent<IProjectile>(out var proj))
                    continue;
                
                if (!proj.IsEnemy)
                    continue;

                //Straight back toward the source - consistent with image and shield behaviour
                Vector3 reflectBase = -proj.Rb.velocity.normalized;
                reflectBase.y = 0f;
                reflectBase.Normalize();
                
                SpawnReflectedSpread(
                    col.transform.position, reflectBase,
                    proj.Rb.velocity.magnitude, ctx, source, args);
                
                Destroy(col.gameObject);
            }
        }

        private void SpawnReflectedSpread(Vector3 origin, Vector3 baseDir, float speed, SpellContext ctx,
            SpellInstance source, DashActivationArgs args)
        {
            var dirs = ReflectionUtils.GetSpreadDirections(
                baseDir, args.ReflectCount, args.ReflectSpread);

            foreach (var d in dirs)
            {
                var go = Instantiate(
                    _reflectedProjectilePrefab, origin, Quaternion.LookRotation(d));
                
                //Reflected projectiles inherit all OnHit runes, no BounceCastRune context
                go.Init(source, d, speed, _baseDamage, ctx.Runner, AbilityType.Projectile, true);
                go.SetPierceCount(0);
                go.SetBounceCount(0);
            }
        }

        public override void StartHold(SpellContext ctx)
        {
        }

        public override void StopHold(SpellContext ctx)
        {
        }

        public override void HoldTick(SpellContext ctx, float delta)
        {
        }
    }
}