using System.Collections;
using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Dash")]
    public sealed class DashAbilityRune : AbilityRuneSO
    {
        [SerializeField] private float _dashSpeed = 20f;
        [SerializeField] private float _baseDashDuration = 0.2f;
        [SerializeField] private float _cooldownDuration = 0.8f;
        [SerializeField] private float _cameraTrauma = 0.5f;
        [SerializeField] private int _baseDamage = 8; // used when DamagesOnDash

        public override AbilityType Type => AbilityType.Dash;
        public override bool IsHoldAbility => false;
        public override float CooldownDuration => _cooldownDuration;

        public override void Activate(SpellContext ctx)
        { }

        internal void ActivateWithInstance(SpellContext ctx, SpellInstance source)
        {
            var player = (PlayerController)ctx.Runner;
            float duration = _baseDashDuration * ctx.Modifiers.DurationMultiplier;
            ctx.Runner.StartCoroutine(DashRoutine(ctx, player, duration, source));
        }

        private IEnumerator DashRoutine(SpellContext ctx, PlayerController player, float duration, SpellInstance source)
        {
            // Determine direction — last input direction, fallback to facing
            Vector2 raw = player.LastInputDirection;
            Vector3 dir = new Vector3(raw.x, 0f, raw.y).normalized;
            if (dir == Vector3.zero) dir = player.transform.forward;

            // Invincibility — distinct from IFrames per locked decisions
            player.SetCanMove(false);
            player.Hurtbox.SetActive(false);

            int bouncesLeft = ctx.Modifiers.BounceCount;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                //Bounce check - raycast a short distance ahead each frame
                if (bouncesLeft > 0)
                {
                    float checkDist = _dashSpeed * Time.fixedDeltaTime * 2f;
                    
                    //Bounce off anything: walls and enemies
                    if (Physics.Raycast(player.transform.position, dir, out RaycastHit hit, checkDist))
                    {
                        dir = Vector3.Reflect(dir, hit.normal);
                        dir.y = 0f; //stay on XZ plane
                        dir = dir.normalized;
                        bouncesLeft--;
                        
                        //Damage enemy on bounce contact
                        if (ctx.Modifiers.DamagesOnDash &&
                            hit.collider.TryGetComponent<IDamageable>(out var dmg))
                        {
                            DamageSystem.Deal(dmg, hit.collider.gameObject, _baseDamage, source.SpellElement);
                            // dmg.TakeDamage(_baseDamage, ElementType.Neutral);
                            if (hit.collider.TryGetComponent<DamageFlash>(out var flash))
                                flash.Flash();
                        }
                    }
                }
                
                player.Rigidbody.velocity = dir * _dashSpeed;
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            player.Rigidbody.velocity = Vector3.zero;
            player.SetCanMove(true);
            player.Hurtbox.SetActive(true);

            // Damage at dash END — base dash has none unless DamagesOnDash is set.
            // PiercingCastRune sets DamagesOnDash = true in Phase 3.
            if (ctx.Modifiers.DamagesOnDash)
                DealEndDamage(player, ctx, source);
        }

        private void DealEndDamage(PlayerController player, SpellContext ctx, SpellInstance source)
        {
            // PROTOTYPE: direct damage call, replace with DamageSystem in Phase 5
            var hits = Physics.OverlapSphere(
                player.transform.position, 2f,
                player.Stats.EnemyLayerMask);

            bool hitAny = false;
            
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<IDamageable>(out var dmg))
                    continue;
                
                DamageSystem.Deal(dmg, hit.gameObject,  _baseDamage, source.SpellElement);
                // dmg.TakeDamage(_baseDamage, ElementType.Neutral);
                
                if (hit.TryGetComponent<DamageFlash>(out var flash)) 
                    flash.Flash();

                //TriggerOnHit per enemy - gives Dot and Knockback a real target
                source.TriggerOnHit(player.transform.position, hit.gameObject, ctx.Runner);
                hitAny = true;
            }

            if (hitAny) HitStop.Apply(0.06f);
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