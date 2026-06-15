using UnityEngine;
using Core;

public class HealthVisualManager : MonoBehaviour
{
    [Header("Execution Parameters")]
    [SerializeField] private PlayerHealth playerHealth; // Reference to the player's health component
    [SerializeField] private ParticleSystem healthRecoveryPrefab; // The green VFX prefab
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1f, 0); // Centers it around the character body
    [SerializeField] private bool attachToCharacter = true; // Does it move with the character?

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealed += PlayRecoveryVisual;
    }
    
    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealed -= PlayRecoveryVisual;
    }
    
    /// <summary>
    /// Triggers the green health recovery visual elements.
    /// </summary>
    public void PlayRecoveryVisual(int amount = 0)
    {
        if (healthRecoveryPrefab == null)
        {
            Debug.LogWarning("Health Recovery Prefab is missing!");
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

        // 5. Clean up memory once the particles are done blowing
        Destroy(vfxInstance.gameObject, vfxInstance.main.duration + vfxInstance.main.startLifetime.constantMax);
    }
}