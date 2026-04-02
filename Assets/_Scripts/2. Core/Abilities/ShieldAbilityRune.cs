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
        
        //Private backing fields - written by cast runes via the interface
        private float _activeRadiusMultiplier;
        private bool _activeAllowEnemyThrough;
        private bool _activeReflectsProjectiles;
        private int _activeReflectCount;
        private float _activeReflectSpread;

        // PROTOTYPE: single-player only. Mutable state on SO is safe here
        // because there is exactly one PlayerController and one shield slot.
        // Replace with per-instance state bag when multiplayer or pooling is needed.
        private float _timeHeld;
        private bool _active;
        private GameObject _shieldVisual;

        internal void ConfigureAndStartHold(SpellContext ctx, SpellInstance source)
        {
            var player = (PlayerController)ctx.Runner;
            if (!player.Energy.TryStartDrain())
                return;

            _active = true;
            _timeHeld = 0f;

            //Instantiate once
            if (!_shieldVisual)
            {
                _shieldVisual = Instantiate(
                    _shieldVisualPrefab,
                    player.transform.position + new Vector3(-0.2f, 1f, 1f),
                    Quaternion.identity,
                    player.transform);

                _shieldVisual.SetActive(false); // prevent events during setup

                var shield = _shieldVisual.GetComponent<ShieldCollider>();
                shield.Bind(source, ctx.Runner);

                // Subscribe events once — they capture source correctly
                shield.OnProjectileAbsorbed += (pos, target) =>
                    source.TriggerOnHit(pos, target, ctx.Runner);
                shield.OnEnemyBodyContact += (pos, target) =>
                    source.TriggerOnHit(pos, target, ctx.Runner);
            }
            
            //Update every activation - rune recipe may have changed
            _shieldVisual.transform.localScale = Vector3.one * _activeRadiusMultiplier;

            var shieldCollider = _shieldVisual.GetComponent<ShieldCollider>();
            shieldCollider.ReflectsProjectiles = _activeReflectsProjectiles;
            shieldCollider.ReflectCount = _activeReflectCount;
            shieldCollider.ReflectSpread = _activeReflectSpread;
            
            //Piercing toggles trigger mode - update every activation
            var col = _shieldVisual.GetComponent<Collider>();
            col.isTrigger = _activeAllowEnemyThrough;

            var damageZone = _shieldVisual.GetComponent<ShieldDamageZone>();
            if (damageZone != null)
                damageZone.Active = _activeAllowEnemyThrough;
            
            _shieldVisual.SetActive(true);
        }

        public override void StartHold(SpellContext ctx)
        {
            ConfigureAndStartHold(ctx, ctx.Source as SpellInstance);
        }

        public override void HoldTick(SpellContext ctx, float deltaTime)
        {
            if (!_active) return;
            
            var player = (PlayerController)ctx.Runner;

            //PlayerEnergy.Tick() already handles draining at stats rate.
            //Shield just watches for depletion and reacts.
            if (player.Energy.IsBroken)
            {
                Deactivate(player);
                return;
            }

            _timeHeld += deltaTime;
            if (_timeHeld >= _abilityThreshold)
            {
                //Fire ability here. No abilities yet.
                _timeHeld -= _abilityThreshold;
            }
        }

        public override void StopHold(SpellContext ctx)
            => Deactivate((PlayerController)ctx.Runner);

        private void Deactivate(PlayerController player)
        {
            _active = false;
            _timeHeld = 0f;
            player.Energy.StopDrain();
            if (_shieldVisual)
                _shieldVisual.SetActive(false);
        }

        public override void ResetActiveConfig()
        {
            _activeRadiusMultiplier = 1f;
            _activeAllowEnemyThrough = false;
            _activeReflectsProjectiles = false;
            _activeReflectCount = 0;
            _activeReflectSpread = 0;
        }

        public override void Activate(SpellContext ctx)
        {
        }
    }
}