using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Projectile")]
    public class ProjectileAbilityRune : AbilityRuneSO, IProjectileConfig
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private int _baseDamage = 10;
        [SerializeField] private float _windupDuration = 0.08f;
        [SerializeField] private float _cooldownDuration = 0.4f; // 1f / fireRate
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.5f;

        public override AbilityType Type => AbilityType.Projectile;
        public override bool IsHoldAbility => false;
        public override float CooldownDuration => _cooldownDuration;
        
        //IProjectileConfig - cast runes write here
        int IProjectileConfig.PierceCount {set => _activePierceCount = value;}
        int IProjectileConfig.BounceCount {set => _activeBounceCount = value;}
        float IProjectileConfig.SizeMultiplier {set => _activeSizeMultiplier = value;}
        int IProjectileConfig.HomingCount {set => _activeHomingCount = value;}
        
        //Private backing fields - written by cast runes via the interface
        private int _activePierceCount = 0;
        private int _activeBounceCount = 0;
        private float _activeSizeMultiplier = 1f;
        private int _activeHomingCount = 0;
        
        public override void Activate(SpellContext ctx)
        {
            //Cast runes already ran - active config is populated
            ctx.Runner.StartCoroutine(WindUpThenFire(ctx));
        }

        private IEnumerator WindUpThenFire(SpellContext ctx)
        {
            yield return Helpers.GetWait(_windupDuration);
            Fire(ctx);
        }

        private void Fire(SpellContext ctx)
        {
            Ray ray = Helpers.GetCamera().ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out var hit, 200f, LayerMask.GetMask("Floor")))
            {
                Debug.LogError($"{nameof(ProjectileAbilityRune)}: Raycast Failed");
                return;
            }

            Vector3 dir = (hit.point - ctx.Runner.transform.position);
            dir.y = 0f;
            dir.Normalize();
            
            if (dir == Vector3.zero)
            {
                Debug.LogError($"{nameof(ProjectileAbilityRune)}: Direction is zero");
                return;
            }

            var go = Instantiate(_projectilePrefab, ctx.Runner.transform.position, Quaternion.LookRotation(dir));
            go.gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
            go.Init(ctx.Source as SpellInstance, dir, _projectileSpeed, _baseDamage, _hitStopDuration, _cameraTrauma,
                ctx.Runner, AbilityType.Projectile, excludeBounceCastRuneForOnHitContext: false);
            go.SetPierceCount(_activePierceCount);
            go.SetBounceCount(_activeBounceCount);
            
            if (_activeSizeMultiplier != 1f)
            {
                go.transform.GetChild(0).localScale = Vector3.one * _activeSizeMultiplier;
                
                var col = go.GetComponent<SphereCollider>();
                if (col != null)
                    col.radius *= _activeSizeMultiplier / 2;
            }

            if (_activeHomingCount > 0)
                SpawnHomingProjectiles(ctx, dir);
        }

        private void SpawnHomingProjectiles(SpellContext ctx, Vector3 dir)
        {
            if (ctx.Source is not SpellInstance si) return;

            foreach (var castRune in si.Recipe.CastRunes())
            {
                if (castRune is not HomingCastRune homing) continue;

                homing.SpawnHomingProjectiles(
                    _activeHomingCount,
                    ctx.Runner.transform.position,
                    dir,
                    ctx.Source.SpellElement,
                    ctx.Runner);
                
                break;
            }
        }

        //Reset must happen at the start of every Activate so stale values
        //never carry over between casts. Called before FireCastRunes
        public override void ResetActiveConfig()
        {
            _activePierceCount = 0;
            _activeBounceCount = 0;
            _activeSizeMultiplier = 1f;
            _activeHomingCount = 0;
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