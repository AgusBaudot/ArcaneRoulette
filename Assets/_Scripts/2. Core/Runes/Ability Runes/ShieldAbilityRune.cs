using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Shield")]
    public class ShieldAbilityRune : AbilityRuneSO, IShieldConfig
    {
        [SerializeField] private GameObject _shieldVisualPrefab;
        [SerializeField] private GameObject _shockwavePrefab;
        [SerializeField] private float _abilityThreshold = 1.5f; // seconds held to spawn shockwave

        public override AbilityType Type => AbilityType.Shield;
        public override bool IsHoldAbility => true;
        public override float CooldownDuration => 0f;
        
        //IShieldConfig
        float IShieldConfig.RadiusMultiplier { set => _activeRadiusMultiplier = value; }
        bool IShieldConfig.AllowEnemyThrough { set => _activeAllowEnemyThrough = value; }
        bool IShieldConfig.ReflectsProjectiles { set => _activeReflectsProjectiles = value; }
        int IShieldConfig.ReflectCount { set => _activeReflectCount = value; }
        float IShieldConfig.ReflectSpread { set => _activeReflectSpread = value; }
        int IShieldConfig.HomingCount { set => _activeHomingCount = value; }
        
        //Private backing fields - written by cast runes via the interface
        private float _activeRadiusMultiplier;
        private bool _activeAllowEnemyThrough;
        private bool _activeReflectsProjectiles;
        private int _activeReflectCount;
        private float _activeReflectSpread;
        private int _activeHomingCount = 0;
        
        //One visual per HoldSpellInstance - keyed by ISpellSource identity
        //Cleared on eun end when SpellInstances are dismantled
        private readonly Dictionary<ISpellSource, GameObject> _visuals = new();

        internal void ConfigureAndStartHold(SpellContext ctx, SpellInstance source)
        {
            var player = (PlayerController)ctx.Runner;
            if (!player.Energy.TryStartDrain())
                return;

            var state = ctx.Source.ShieldState;
            state.Active = true;
            state.TimeHeld = 0f;

            // ── Instantiate once per source instance ────────────────────────────
            if (!_visuals.TryGetValue(ctx.Source, out var visual) || visual == null)
            {
                visual = Instantiate(
                    _shieldVisualPrefab,
                    player.transform.position + new Vector3(-0.2f, 1f, 1f),
                    Quaternion.identity,
                    player.transform);

                visual.SetActive(false); // prevent events during setup
                _visuals[ctx.Source] = visual;
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
            visual.transform.localScale = Vector3.one * _activeRadiusMultiplier;
            
            shield.ReflectsProjectiles = _activeReflectsProjectiles;
            shield.ReflectCount = _activeReflectCount;
            shield.ReflectSpread = _activeReflectSpread;
            
            //Piercing toggles trigger mode - update every activation
            var col = visual.GetComponent<Collider>();
            col.isTrigger = _activeAllowEnemyThrough;

            var damageZone = visual.GetComponent<ShieldDamageZone>();
            if (damageZone != null)
                damageZone.Active = _activeAllowEnemyThrough;
            
            visual.SetActive(true);

            if (_activeHomingCount > 0)
                SpawnHomingFromShield(ctx);
        }

        public override void StartHold(SpellContext ctx)
        {
            ConfigureAndStartHold(ctx, ctx.Source as SpellInstance);
        }

        public override void HoldTick(SpellContext ctx, float deltaTime)
        {
            var state = ctx.Source.ShieldState;
            if (!state.Active) return;
            
            var player = (PlayerController)ctx.Runner;

            //PlayerEnergy.Tick() already handles draining at stats rate.
            //Shield just watches for depletion and reacts.
            if (player.Energy.IsBroken)
            {
                StopHold(ctx);
                return;
            }

            state.TimeHeld += deltaTime;
            if (state.TimeHeld >= _abilityThreshold)
            {
                //Fire ability here. No abilities yet.
                state.TimeHeld -= _abilityThreshold;
            }
        }

        public override void StopHold(SpellContext ctx)
        {
            var state = ctx.Source.ShieldState;
            state.Active = false;
            state.TimeHeld = 0f;
            
            ((PlayerController)ctx.Runner).Energy.StopDrain();
            
            if (_visuals.TryGetValue(ctx.Source, out var visual) && visual != null)
                visual.SetActive(false);
        }

        public override void ResetActiveConfig()
        {
            _activeRadiusMultiplier = 1f;
            _activeAllowEnemyThrough = false;
            _activeReflectsProjectiles = false;
            _activeReflectCount = 0;
            _activeReflectSpread = 0f;
            _activeHomingCount = 0;
        }

        public override void Activate(SpellContext ctx)
        {
        }

        //Called by SpellCrafter.Dismantle - cleans up the visual for this instance
        public void CleanupInstance(ISpellSource source)
        {
            if (_visuals.TryGetValue(source, out var visual) && visual != null)
                Destroy(visual);
            _visuals.Remove(source);
        }

        private void SpawnHomingFromShield(SpellContext ctx)
        {
            if (ctx.Source is not SpellInstance si)
                return;
            
            Vector3 dir = ctx.Runner.transform.forward;

            foreach (var castRune in si.Recipe.CastRunes())
            {
                if (castRune is not HomingCastRune homing)
                    continue;
                
                homing.SpawnHomingProjectiles(
                    _activeHomingCount,
                    ctx.Runner.transform.position,
                    dir,
                    ctx.Source.SpellElement,
                    ctx.Runner);

                break;
            }
        }
    }
}