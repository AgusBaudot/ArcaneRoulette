using Foundation;
using UnityEngine;

namespace Core
{
    public class PlayerEnergy : MonoBehaviour, IUpdatable
    {
        public float Current { get; private set; }
        public float Max => _stats.MaxEnergy;
        public bool IsBroken { get; private set; } //true = depleted, must fully restore.
        
        //IUpdatable
        public int UpdatePriority => Foundation.UpdatePriority.Player;

        private PlayerStats _stats;
        private bool _isDraining;

        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            Current = stats.MaxEnergy;
        }
        
        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance.Unregister(this);
        }

        public void Tick(float dt)
        {
            if (_isDraining)
            {
                Current = Mathf.Max(0f, Current - _stats.EnergyDrainRate * dt);

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
                Current = Mathf.Min(Max, Current + _stats.EnergyRestoreRate * dt);

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