using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Runes/Ability/Shield")]
    public sealed class ShieldAbilityRune : AbilityRuneSO
    {
        [Header("Stats")]
        [SerializeField] private GameObject _defaultShieldPrefab;
        [SerializeField] private ElementalGameObject[] _elementalShields;
        [SerializeField] private GameObject _shockwavePrefab;
        [SerializeField] private float _abilityThreshold = 1.5f; // seconds held to spawn shockwave
        
        [Header("Audio")]
        [SerializeField] private AudioEventSO _defaultCastSound;
        [SerializeField] private ElementalSound[] _elementalSounds;

        // ── Implemented Abstract Properties ───────────────────────────────────
        public override AbilityType Type => AbilityType.Shield;
        public override bool IsHoldAbility => true;
        public override float CooldownDuration => 0f;

        public override void Activate(SpellContext ctx) 
        { 
            // Intentionally empty — handled by Hold interface
        }

        public override void StartHold(SpellContext ctx)
        {
            if (ctx.Source is not HoldSpellInstance hold) return;

            var args = new ShieldActivationArgs
            {
                AllowEnemyThrough = false,
                ReflectsProjectiles = false,
                ReflectCount = 0,
                ReflectSpread = 0f,
                RadiusMultiplier = 1f,
                HomingCount = 0
            };

            (ctx.Source as ISpellEventSource)?.RaiseBeforeStartHold(args);

            ConfigureAndStartHold(ctx, hold, args);
        }

        private void ConfigureAndStartHold(SpellContext ctx, HoldSpellInstance hold, ShieldActivationArgs args)
        {
            if (!hold.Energy.TryStartDrain()) return;

            if (hold.ActiveShieldVisual == null)
            {
                GameObject prefabToSpawn = GetShieldPrefab(ctx.AttackerElement);
                if (prefabToSpawn != null)
                {
                    var player = (PlayerController)ctx.Runner;
                    
                    hold.ActiveShieldVisual = Instantiate(
                        prefabToSpawn,
                        player.transform.position + new Vector3(-0.2f, 1f, 1f),
                        Quaternion.identity,
                        player.transform);

                    hold.ActiveShieldVisual.SetActive(false);

                    if (hold.ActiveShieldVisual.TryGetComponent<ShieldCollider>(out var shieldCollider))
                    {
                        shieldCollider.Bind(hold, ctx.Runner);
                        shieldCollider.UnsubscribeListeners();
                        
                        shieldCollider.OnProjectileAbsorbed += (pos, target) =>
                            hold.TriggerOnHit(pos, target, ctx.Runner);
                        shieldCollider.OnEnemyBodyContact += (pos, target) =>
                            hold.TriggerOnHit(pos, target, ctx.Runner);
                        shieldCollider.OnShieldDamaged += player.DamageShield;
                    }
                }
            }

            if (hold.ActiveShieldVisual != null)
            {
                hold.ActiveShieldVisual.transform.localScale = Vector3.one * args.RadiusMultiplier;

                if (hold.ActiveShieldVisual.TryGetComponent<ShieldCollider>(out var shield))
                {
                    shield.ReflectsProjectiles = args.ReflectsProjectiles;
                    shield.ReflectCount = args.ReflectCount;
                    shield.ReflectSpread = args.ReflectSpread;
                }

                if (hold.ActiveShieldVisual.TryGetComponent<Collider>(out var col))
                {
                    col.isTrigger = args.AllowEnemyThrough;
                }

                if (hold.ActiveShieldVisual.TryGetComponent<ShieldDamageZone>(out var dmgZone))
                {
                    dmgZone.Active = args.AllowEnemyThrough;

                    if (args.AllowEnemyThrough)
                    {
                        dmgZone.Bind(ctx.AttackerElement);
                    }
                }

                hold.ActiveShieldVisual.SetActive(true);
            }

            if (hold.ActiveHoldAudio == null || !hold.ActiveHoldAudio.IsValid)
            {
                AudioEventSO sound = GetHoldSound(ctx.AttackerElement);
                if (sound != null)
                {
                    EventBus.Publish(new AudioPlayTrackedRequest
                    {
                        Event = sound,
                        WorldPosition = ctx.Runner.transform.position,
                        OnHandleReady = handle => hold.ActiveHoldAudio = handle
                    });
                }
            }

            if (args.HomingCount > 0)
            {
                SpawnHomingFromShield(ctx, args.HomingCount);
            }
            
            var state = hold.ShieldState;
            if (state != null)
            {
                state.Active = true;
                state.TimeHeld = 0f;
            }
        }

        public override void StopHold(SpellContext ctx)
        {
            if (ctx.Source is not HoldSpellInstance hold) return;

            var state = hold.ShieldState;
            if (state != null)
            {
                state.Active = false;
                state.TimeHeld = 0f;
            }
            
            hold.Energy.StopDrain();

            if (hold.ActiveShieldVisual != null)
            {
                hold.ActiveShieldVisual.SetActive(false);
            }

            if (hold.ActiveHoldAudio != null && hold.ActiveHoldAudio.IsValid)
            {
                EventBus.Publish(new AudioStopRequest 
                { 
                    Handle = hold.ActiveHoldAudio, 
                    FadeOut = true 
                });
                
                hold.ActiveHoldAudio = default; 
            }
        }

        public override void HoldTick(SpellContext ctx, float dt)
        {
            if (ctx.Source is not HoldSpellInstance hold) return;
            
            var state = hold.ShieldState;
            if (state == null || !state.Active) return;

            if (hold.Energy.IsBroken)
            {
                StopHold(ctx);
                return;
            }
            
            state.TimeHeld += dt;
            if (state.TimeHeld >= _abilityThreshold)
            {
                state.TimeHeld -= _abilityThreshold;
                
                // Shockwave logic utilizing _shockwavePrefab goes here
            }
        }

        private GameObject GetShieldPrefab(ElementType element)
        {
            if (_elementalShields == null || _elementalShields.Length == 0) return _defaultShieldPrefab;
            
            foreach (var mapping in _elementalShields)
            {
                if (mapping.Element == element) return mapping.Reference;
            }
            return _defaultShieldPrefab;
        }

        private AudioEventSO GetHoldSound(ElementType element)
        {
            if (_elementalSounds == null || _elementalSounds.Length == 0) return _defaultCastSound;
            
            foreach (var snd in _elementalSounds)
            {
                if (snd.Element == element) return snd.CastSound;
            }
            return _defaultCastSound;
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
    }
}