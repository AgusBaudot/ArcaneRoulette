using Foundation;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Self-contained energy pool. Owned by HoldSPellInstance - one pool
    /// per equipped hold spell, ticked by HoldSpellInstance.HoldTick.
    /// </summary>
    public sealed class EnergyPool
    {
        public float Current { get; private set; }
        public float Max { get; }
        public bool IsBroken { get; private set; } //true = depleted, must fully restore.

        private readonly float _drainRate;
        private readonly float _restoreRate;
        private readonly float _drainOnStart;
        private readonly float _drainOnHit;
        
        private bool _isDraining;

        public EnergyPool(PlayerStats stats)
        {
            Max = stats.MaxEnergy;
            _drainRate = stats.EnergyDrainRate;
            _restoreRate = stats.EnergyRestoreRate;
            _drainOnStart = stats.DrainOnStart;
            _drainOnHit = stats.EnergyDepletedOnHit;
            Current = Max;
        }
        public void Tick(float dt)
        {
            if (_isDraining)
            {
                Current -= _drainRate * dt;

                if (!(Current <= 0f)) 
                    return;
                    
                Current = 0f;
                IsBroken = true;
                _isDraining = false;
            }
            else
            {
                Current += _restoreRate * dt;
                if (Current > Max)
                    Current = Max;

                if (IsBroken && Current >= Max)
                    IsBroken = false; //only clears on full restore.
            }
        }

        public void DrainOnHit() => Current -= Max * _drainOnHit;

        /// <returns>
        /// False if broken or empty - shield checks this before activating.
        /// </returns>
        public bool TryStartDrain()
        {
            if (IsBroken || Current <= 0f)
                return false;

            Current -= Max * _drainOnStart;

            if (Current < 0f)
                Current = 0f;
            
            _isDraining = true;
            return true;
        }

        public void StopDrain() => _isDraining = false;

        /// <summary>
        /// Bypasses normal drain flow, immediately breaks the shield.
        /// Used by world hazards.
        /// </summary>
        public void ForceDeplete()
        {
            Current = 0f;
            IsBroken = true;
            _isDraining = false;
        }
    }
}