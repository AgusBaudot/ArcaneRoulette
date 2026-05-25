using Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;

public class Cheats : MonoBehaviour, IUpdatable
{
    public int UpdatePriority => Foundation.UpdatePriority.Input;
    
    private readonly string _scene1 = "Core Loop";
    private readonly string _scene2 = "Hardcore Room";
    private PlayerHealth _playerHealth;
    private const int DebugDamageAmount = 1;
    
    private void OnEnable()
    {
        UpdateManager.Instance.Register(this);
        _playerHealth = FindObjectOfType<PlayerHealth>();
    }

    private void OnDisable() => UpdateManager.Instance?.Unregister(this);

    public void Tick(float dt)
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (SceneManager.GetActiveScene().name == _scene1)
                return;
            SceneManager.LoadScene(_scene1);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (SceneManager.GetActiveScene().name == _scene2)
                return;
            SceneManager.LoadScene(_scene2);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_playerHealth == null)
                _playerHealth = FindObjectOfType<PlayerHealth>();

            _playerHealth?.TakeDamage(DebugDamageAmount, ElementType.Neutral);
        }
    }
}
