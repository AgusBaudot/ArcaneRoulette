using UnityEngine;
using UnityEngine.VFX;
using Core;
using Foundation;

public class HealthVisualManager : MonoBehaviour
{
    [Header("Execution Parameters")]
    [SerializeField] private PlayerHealth _playerHealth;
    
    [SerializeField] private PooledVFXGraph _healthRecoveryPrefab; 
    
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0, 1f, 0); 
    [SerializeField] private bool _attachToCharacter = true; 

    private float _previousHp;

    private void OnEnable()
    {
        if (GameStateManager.RunState != null)
        {
            _previousHp = GameStateManager.RunState.CurrentHp;
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
        if (Time.timeSinceLevelLoad < 0.2f)
        {
            _previousHp = currentHp;
            return;
        }

        if (currentHp > _previousHp)
        {
            PlayRecoveryVisual();
        }
        
        _previousHp = currentHp;
    }

    public void PlayRecoveryVisual()
    {
        if (_healthRecoveryPrefab == null)
        {
            Debug.LogWarning("Health Recovery Prefab is missing!");
            return;
        }

        if (_playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth reference is missing! Cannot spawn VFX.");
            return;
        }

        Vector3 spawnPosition = _playerHealth.transform.position + _spawnOffset;

        PooledVFXGraph vfxInstance = Helpers.ProjFactory.Spawn<PooledVFXGraph>(_healthRecoveryPrefab, spawnPosition, Quaternion.identity);

        if (_attachToCharacter)
        {
            vfxInstance.transform.SetParent(_playerHealth.transform);
        }
        
        VisualEffect vfxGraph = vfxInstance.GetComponent<VisualEffect>();
        if (vfxGraph != null)
        {
            vfxGraph.Play();
        }
    }
}