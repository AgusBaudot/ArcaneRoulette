using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Dash")]
    public sealed class DashAbilityRune : AbilityRuneSO, IDashConfig
    {
        //IDashConfig
        float IDashConfig.DurationMultiplier { set => _activeDurationMultiplier = value; }
        bool IDashConfig.DamagesOnDash { set => _activeDamagesOnDash = value; }
        bool IDashConfig.ReflectsProjectiles { set => _activeReflectsProjectiles = value; }
        int IDashConfig.BounceCount { set =>  _activeBounceCount = value; }
        int IDashConfig.ReflectCount { set => _activeReflectCount = value; }
        float IDashConfig.ReflectSpread { set => _activeReflectSpread = value; }
        
        [SerializeField] private float _dashSpeed = 20f;
        [SerializeField] private float _baseDashDuration = 0.2f;
        [SerializeField] private float _cooldownDuration = 0.8f;
        [SerializeField] private float _cameraTrauma = 0.5f;
        [SerializeField] private int _baseDamage = 8;
        [SerializeField] private float _dashHitRadius = 0.8f;
        [SerializeField] private Projectile _reflectedProjectilePrefab;
        [SerializeField] private float _reflectHitStop = 0.06f;
        [SerializeField] private float _reflectTrauma = 0.8f;
        

        public override AbilityType Type => AbilityType.Dash;
        public override bool IsHoldAbility => false;
        public override float CooldownDuration => _cooldownDuration;

        private float _activeDurationMultiplier;
        private bool _activeDamagesOnDash;
        private bool _activeReflectsProjectiles;
        private int _activeBounceCount;
        private int _activeReflectCount;
        private float _activeReflectSpread;

        public override void Activate(SpellContext ctx)
        {
            ctx.Runner.StartCoroutine(DashRoutine(ctx, (PlayerController)ctx.Runner,
                _baseDashDuration * _activeDurationMultiplier));
        }

        private IEnumerator DashRoutine(SpellContext ctx, PlayerController player, float duration)
        {
            // Determine direction — last input direction, fallback to facing
            Vector2 raw = player.LastInputDirection;
            Vector3 dir = new Vector3(raw.x, 0f, raw.y).normalized;
            if (dir == Vector3.zero) dir = player.transform.forward;

            // Invincibility — distinct from IFrames per locked decisions
            player.SetCanMove(false);
            player.Hurtbox.SetActive(false);

            int bouncesLeft = _activeBounceCount;
            float elapsed = 0f;

            var hitEnemies = new HashSet<GameObject>();
            
            while (elapsed < duration)
            {
                //Enemy collision - OnHit per enemy touched
                var enemies = Physics.OverlapSphere(
                    player.transform.position, _dashHitRadius, player.Stats.EnemyLayerMask);

                foreach (var hit in enemies)
                {
                    if (!hitEnemies.Add(hit.gameObject))
                        continue;
                    
                    if (!hit.TryGetComponent<IDamageable>(out var dmg)) 
                        continue;

                    // Damage only if PiercingCastRune is slotted
                    if (_activeDamagesOnDash)
                    {
                        DamageSystem.Deal(dmg, hit.gameObject, _baseDamage, ctx.Source.SpellElement);
                    }

                    // OnHit runes always fire per enemy touched regardless of damage
                    ctx.Source.TriggerOnHit(hit.transform.position, hit.gameObject, ctx.Runner,
                        AbilityType.Dash, false, dir);
                }

                // ── Enemy projectile reflection (Bounce rune) ────────────────────
                if (_activeReflectCount > 0 && _reflectedProjectilePrefab != null)
                {
                    ReflectNearbyProjectiles(player, dir, ctx, ctx.Source as SpellInstance);
                }
                
                player.Rigidbody.velocity = dir * _dashSpeed;
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            player.Rigidbody.velocity = Vector3.zero;
            player.SetCanMove(true);
            player.Hurtbox.SetActive(true);
        }

        private void ReflectNearbyProjectiles(PlayerController player, Vector3 dashDir, SpellContext ctx,
            SpellInstance source)
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
                    proj.Rb.velocity.magnitude, ctx, source);
                
                Destroy(col.gameObject);
            }
        }

        private void SpawnReflectedSpread(Vector3 origin, Vector3 baseDir, float speed, SpellContext ctx,
            SpellInstance source)
        {
            var dirs = ReflectionUtils.GetSpreadDirections(
                baseDir, _activeReflectCount, _activeReflectSpread);

            foreach (var d in dirs)
            {
                var go = Instantiate(
                    _reflectedProjectilePrefab, origin, Quaternion.LookRotation(d));
                
                //Reflected projectiles inherit all OnHit runes, no BounceCastRune context
                go.Init(source, d, speed, _baseDamage, _reflectHitStop, _reflectTrauma, ctx.Runner, AbilityType.Projectile, true);
                go.SetPierceCount(0);
                go.SetBounceCount(0);
            }
        }

        public override void ResetActiveConfig()
        {
            _activeDurationMultiplier = 01f;
            _activeDamagesOnDash = false;
            _activeReflectsProjectiles = false;
            _activeBounceCount = 0;
            _activeReflectCount = 0;
            _activeReflectSpread = 0;
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