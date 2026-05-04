using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Shield")]
    public class ShieldAbilityRune : AbilityRuneSO
    {
        [SerializeField] private GameObject _shieldVisualPrefab;
        [SerializeField] private GameObject _shockwavePrefab;
        [SerializeField] private float _abilityThreshold = 1.5f; // seconds held to spawn shockwave

        public override AbilityType Type => AbilityType.Shield;
        public override bool IsHoldAbility => true;
        public override float CooldownDuration => 0f;
        
        //One visual per HoldSpellInstance - keyed by ISpellSource identity
        //Cleared on eun end when SpellInstances are dismantled
        private readonly Dictionary<ISpellSource, GameObject> _visuals = new();
        
        public override void StartHold(SpellContext ctx)
        {
            var args = new ShieldActivationArgs();
            (ctx.Source as ISpellEventSource)?.RaiseBeforeStartHold(args);
            ConfigureAndStartHold(ctx, ctx.Source as HoldSpellInstance, args);
        }

        internal void ConfigureAndStartHold(SpellContext ctx, HoldSpellInstance source, ShieldActivationArgs args)
        {
            if (source == null)
                return;
            
            if (!source.Energy.TryStartDrain())
                return;

            var state = source.ShieldState;
            state.Active = true;
            state.TimeHeld = 0f;

            // ── Instantiate once per source instance ────────────────────────────
            if (!_visuals.TryGetValue(source, out var visual) || visual == null)
            {
                var player = (PlayerController)ctx.Runner;
                
                visual = Instantiate(
                    _shieldVisualPrefab,
                    player.transform.position + new Vector3(-0.2f, 1f, 1f),
                    Quaternion.identity,
                    player.transform);

                visual.SetActive(false); // prevent events during setup
                _visuals[source] = visual;
            }
            
            // ── Wire every activation — source may have changed ─────────────────
            var shield = visual.GetComponent<ShieldCollider>();
            shield.Bind(source, ctx.Runner);

            // Unsubscribe previous listeners before re-subscribing.
            shield.UnsubscribeListeners();

            shield.OnProjectileAbsorbed += (pos, target) =>
                source.TriggerOnHit(pos, target, ctx.Runner);
            shield.OnEnemyBodyContact += (pos, target) =>
                source.TriggerOnHit(pos, target, ctx.Runner);
            
            // ── Update collider properties every activation ──────────────────────
            visual.transform.localScale = Vector3.one * args.RadiusMultiplier;
            
            shield.ReflectsProjectiles = args.ReflectsProjectiles;
            shield.ReflectCount = args.ReflectCount;
            shield.ReflectSpread = args.ReflectSpread;
            
            //Piercing toggles trigger mode - update every activation
            var col = visual.GetComponent<Collider>();
            col.isTrigger = args.AllowEnemyThrough;

            var damageZone = visual.GetComponent<ShieldDamageZone>();
            
            if (damageZone != null)
                damageZone.Active = args.AllowEnemyThrough;
            
            visual.SetActive(true);

            if (args.HomingCount > 0)
                SpawnHomingFromShield(ctx, args.HomingCount);
        }

        public override void HoldTick(SpellContext ctx, float deltaTime)
        {
            var source = ctx.Source as HoldSpellInstance;
            if (source == null)
                return;
            
            var state = source.ShieldState;
            if (!state.Active)
                return;

            //PlayerEnergy.Tick() already handles draining at stats rate.
            //Shield just watches for depletion and reacts.
            if (source.Energy.IsBroken)
            {
                StopHold(ctx);
                return;
            }

            state.TimeHeld += deltaTime;
            if (state.TimeHeld >= _abilityThreshold)
                state.TimeHeld -= _abilityThreshold;
        }

        public override void StopHold(SpellContext ctx)
        {
            var source = ctx.Source as HoldSpellInstance;
            if (source == null)
                return;
            
            var state = ctx.Source.ShieldState;
            state.Active = false;
            state.TimeHeld = 0f;
            
            source.Energy.StopDrain();
            
            if (_visuals.TryGetValue(source, out var visual) && visual != null)
                visual.SetActive(false);
        }

        //Called by SpellCrafter.Dismantle - cleans up the visual for this instance
        public void CleanupInstance(ISpellSource source)
        {
            if (_visuals.TryGetValue(source, out var visual) && visual != null)
                Destroy(visual);
            
            _visuals.Remove(source);
        }

        private void SpawnHomingFromShield(SpellContext ctx, int count)
        {
            if (ctx.Source is not SpellInstance si)
                return;
            
            Vector3 dir = ctx.Runner.transform.forward;

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
        
        public override void Activate(SpellContext ctx)
        {
        }
    }
}