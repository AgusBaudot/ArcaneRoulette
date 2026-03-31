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

        // PROTOTYPE: single-player only. Mutable state on SO is safe here
        // because there is exactly one PlayerController and one shield slot.
        // Replace with per-instance state bag when multiplayer or pooling is needed.
        private float _timeHeld;
        private bool _active;
        private bool _allowEnemyThrough;
        private GameObject _shieldVisual;

        internal void StartHoldWithInstance(SpellContext ctx, SpellInstance source)
        {
            var player = (PlayerController)ctx.Runner;
            if (!player.Energy.TryStartDrain())
                return;

            _active = true;
            _timeHeld = 0f;

            if (!_shieldVisual)
            {
                _shieldVisual = Instantiate(_shieldVisualPrefab, player.transform.position + new Vector3(-0.2f, 1f, 1f),
                    Quaternion.identity, player.transform);
                // Prevent trigger/collision events during the setup/bind phase.
                _shieldVisual.SetActive(false);
                _shieldVisual.transform.localScale = Vector3.one * ctx.Modifiers.RadiusMultiplier;

                var shield = _shieldVisual.GetComponent<ShieldCollider>();
                shield.ReflectsProjectiles = ctx.Modifiers.ReflectsProjectiles;
                shield.Bind(source, ctx.Runner);
                
                //Now we have source - TriggerOnHit is wired correctly
                shield.OnProjectileAbsorbed += (pos, target) =>
                    source.TriggerOnHit(pos, target, ctx.Runner);
                shield.OnProjectileReflected += (pos, target) =>
                {
                    // Bounce reflection moment (Collision 1):
                    // suppress full shield OnHit rune set here.
                    // Reflected projectile hits will trigger the OnHit runes in Collision 3.
                };
                shield.OnEnemyBodyContact += (pos, target) => 
                    source.TriggerOnHit(pos, target, ctx.Runner);

                //After instantiating shield visual, alongside ShieldCollder wiring:
                var damageZone = _shieldVisual.GetComponent<ShieldDamageZone>();
                if (damageZone != null)
                    damageZone.Active = ctx.Modifiers.AllowEnemyThrough;
                
                if (ctx.Modifiers.AllowEnemyThrough)
                    _shieldVisual.GetComponent<Collider>().isTrigger = true;
            }
            
            _shieldVisual.SetActive(true);
        }

        public override void StartHold(SpellContext ctx)
        { }

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

        public override void Activate(SpellContext ctx)
        {
        }
    }
}