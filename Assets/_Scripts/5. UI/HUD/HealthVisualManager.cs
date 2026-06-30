using UnityEngine;
using Core;
using Foundation;

public class HealthVisualManager : MonoBehaviour
{
    [Header("Execution Parameters")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private ParticleSystem healthRecoveryPrefab; 
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1f, 0); 
    [SerializeField] private bool attachToCharacter = true; 

    private float _previousHp;

    private void OnEnable()
    {
        if (GameStateManager.RunState != null)
        {
            // Snapshot the HP right as this object turns on
            _previousHp = GameStateManager.RunState.CurrentHp;
            
            // Subscribe to the event
            GameStateManager.RunState.OnHpChanged += HandleHpChanged;
        }
    }

    private void OnDisable()
    {
        if (GameStateManager.RunState != null)
        {
            GameStateManager.RunState.OnHpChanged -= HandleHpChanged;
        }
    }

    private void HandleHpChanged(float currentHp, float maxHp)
    {
        // 1. Ignore initialization spikes: 
        // If this happens in the first 0.2 seconds of the scene, it's just the game 
        // applying the player's base stats, NOT an actual mid-game heal.
        if (Time.timeSinceLevelLoad < 0.2f)
        {
            _previousHp = currentHp;
            return;
        }

        // 2. Only play the visual if the health actually increased
        if (currentHp > _previousHp)
        {
            PlayRecoveryVisual();
        }
        
        // 3. Update the previous HP for the next evaluation
        _previousHp = currentHp;
    }

    public void PlayRecoveryVisual()
    {
        if (healthRecoveryPrefab == null)
        {
            Debug.LogWarning("Health Recovery Prefab is missing!");
            return;
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth reference is missing! Cannot spawn VFX.");
            return;
        }

        // 1. Calculate the execution position surrounding the character
        Vector3 spawnPosition = playerHealth.transform.position + spawnOffset;

        // 2. Instantiate the particle system
        ParticleSystem vfxInstance = Instantiate(healthRecoveryPrefab, spawnPosition, Quaternion.identity);

        // 3. Handle tracking (If true, parent it so it moves *with* the character)
        if (attachToCharacter)
        {
            vfxInstance.transform.SetParent(playerHealth.transform);
        }

        // 4. Play the particle system
        vfxInstance.Play();
    }
}