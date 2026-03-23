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
        private GameObject _shieldVisual;

        public override void StartHold(SpellContext ctx)
        {
            var player = (PlayerController)ctx.Runner;
            if (!player.Energy.TryStartDrain()) return;

            _active = true;
            _timeHeld = 0f;

            // RadiusMultiplier written by AmplifyCastRune in Phase 3
            // Shield visual scale can read ctx.Modifiers.RadiusMultiplier here if needed
            if (!_shieldVisual)
            {
                _shieldVisual = Instantiate(_shieldVisualPrefab, player.transform.position + new Vector3(-0.2f, 1, 1), Quaternion.identity, player.transform);
            }
            _shieldVisual.SetActive(true);
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
                Instantiate(_shockwavePrefab, player.transform.position, Quaternion.identity);
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