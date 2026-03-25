using System.Collections;
using UnityEngine;
using Foundation;

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
        [SerializeField] private float _hitStopDuration = 0.06f;
        [SerializeField] private float _cameraTrauma = 0.5f;

        public override AbilityType Type => AbilityType.Projectile;
        public override bool IsHoldAbility => false;
        public override float CooldownDuration => _cooldownDuration;

        //Called by SpellInstance when ability is a ProjectileAbilityRune.
        //SpellInstance passes itself so Projectile can call TriggerOnHit on impact
        internal void ActivateWithInstance(SpellContext ctx, SpellInstance source)
        {
            ctx.Runner.StartCoroutine(WindUpThenFire(ctx, source));
        }

        private IEnumerator WindUpThenFire(SpellContext ctx, SpellInstance source)
        {
            yield return Helpers.GetWait(_windupDuration);
            Fire(ctx, source);
        }

        private void Fire(SpellContext ctx, SpellInstance source)
        {
            //Raycast floor plane for mouse-aimed direction.
            var floorPlane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Helpers.GetCamera().ScreenPointToRay(Input.mousePosition);

            if (!floorPlane.Raycast(ray, out float distance))
                return;

            Vector3 dir = (ray.GetPoint(distance) - ctx.Runner.transform.position).normalized;
            if (dir == Vector3.zero)
                return;

            var go = Instantiate(_projectilePrefab, ctx.Runner.transform.position, Quaternion.LookRotation(dir));
            go.Init(source, dir, _projectileSpeed, _baseDamage, _hitStopDuration, _cameraTrauma,
                ctx.Runner, AbilityType.Projectile, excludeBounceCastRuneForOnHitContext: false);
            go.SetPierceCount(ctx.Modifiers.PierceCount);
            go.SetBounceCount(ctx.Modifiers.BounceCount);
            go.transform.localScale = Vector3.one * ctx.Modifiers.SizeMultiplier;
            //Also scale the collider if it's a SphereCollider
            var col = go.GetComponent<SphereCollider>();
            if (col) 
                col.radius *= ctx.Modifiers.SizeMultiplier;
        }

        //Called by SpellInstance for non-projectile abilities.
        //Projectile path goes through ActivateWithInstance - see SpellInstance.Activate().
        public override void Activate(SpellContext ctx)
        {
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