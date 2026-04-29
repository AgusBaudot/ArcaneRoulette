using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Movement")] 
        [Tooltip("The base speed at which the player moves.")]
        public float BaseSpeed = 6f;

        [Tooltip("How fast can the player reach max speed.")]
        public float Acceleration = 20f;

        [Tooltip("How fast does the player lose momentum when not moving.")]
        public float Deceleration = 25f;
        
        [Tooltip("How many hitpoints does the player have.")]
        [Range(0, 100)]
        public int BaseHp = 100;

        [Header("Dash")] 
        [Tooltip("The base speed at which the player dashes.")]
        public float DashSpeed = 15f;

        [Tooltip("The base duration of the dash.")]
        public float DashDuration = 0.2f;

        [Tooltip("The base cooldown of the dash.")]
        public float DashCooldown = 1f;

        [Header("Attack")] 
        [Tooltip("The base damage the player does.")]
        public int BaseDamage = 10;

        [Tooltip("The base fire-rate the player has.")]
        public float FireRate = 1.5f;

        [Tooltip("The base projectile speed.")]
        public float ProjectileSpeed = 3;

        [Tooltip("The amount of time  it takes the player to cast this attack.")]
        public float Windup = 0.5f;

        [Header("Defense")] 
        [Tooltip("Max amount of energy the shield has.")]
        public float MaxEnergy = 100f;
        [Tooltip("The rate at which the energy decreases when shield is being used.")]
        public float EnergyDrainRate = 20f;
        [Tooltip("Normalized between 0 and 1. 0.5 = 50%")] [Range(0, 1)]
        public float _drainOnStart = 0.2f;
        [Tooltip("The rate at which the energy increases when shield is not being used.")]
        public float EnergyRestoreRate = 15f;
        [Tooltip("Percentage of damage blocked")]
        public int Blckage = 100;

        [Header("Input")] [Tooltip("Which key will trigger the player's 1st slot.")]
        public KeyCode Slot1 = KeyCode.Mouse0;

        [Tooltip("Which key will trigger the player's 2nd slot.")]
        public KeyCode Slot2 = KeyCode.LeftShift;

        [Tooltip("Which key will trigger the player's 3rd slot.")]
        public KeyCode Slot3 = KeyCode.Space;
        
        public KeyCode[] SlotKeys;
        
        [Header("IFrames")]
        [Tooltip("The amount of time that invincibility frames last.")]
        public float IFrameDuration = 0.8f;

        [Tooltip("The amount of time the player flashes.")]
        public float IFrameFlashInterval = 0.08f;

        [Header("Layers")] [Tooltip("The layer that the player is on.")]
        public LayerMask PlayerLayerMask;

        [Tooltip("The layer that the enemies are on.")]
        public LayerMask EnemyLayerMask;

        [Tooltip("The layer that the spells are on.")]
        public LayerMask SpellLayerMask;

        [Tooltip("The layer that the floor is on.")]
        public LayerMask FloorLayerMask;

        [Tooltip("The layer that the dash-able objects are on.")]
        public LayerMask DashableLayerMask;

        [Tooltip("The layer that the non-trespassable objects are on.")]
        public LayerMask ObstaclesLayerMask;
    }
}