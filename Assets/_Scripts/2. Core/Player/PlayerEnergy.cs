using Unity.VisualScripting;
using UnityEngine;

namespace Core
{
    public class PlayerEnergy : MonoBehaviour
    {
        public float Current { get; private set; }
        public float Max => _stats.MaxEnergy;
        public bool IsBroken { get; private set; } //true = depleted, must fully restore.

        private PlayerStats _stats;
        private bool _isDraining;

        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            Current = stats.MaxEnergy;
        }

        //UpdateManager: replace Update() with Tick()
        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (_isDraining)
            {
                Current = Mathf.Max(0f, Current - _stats.EnergyDrainRate * deltaTime);

                if (Current <= 0f)
                {
                    Current = 0f;
                    IsBroken = true;
                    _isDraining = false;
                    //EventBus.Publish(new EnergyDepletedEvent());
                }
            }
            else
            {
                Current = Mathf.Min(Max, Current + _stats.EnergyRestoreRate * deltaTime);

                if (IsBroken && Current >= Max)
                    IsBroken = false; //only clears on full restore.
            }
        }

        /// <summary>
        /// Returns false if broken or empty - shield checks this before activating.
        /// </summary>
        /// <returns></returns>
        public bool TryStartDrain()
        {
            if (IsBroken || Current <= 0f)
                return false;

            _isDraining = true;
            return true;
        }

        public void StopDrain() => _isDraining = false;
    }
}