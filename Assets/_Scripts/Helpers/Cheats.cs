using Foundation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Cheats : MonoBehaviour, IUpdatable
{
    public int UpdatePriority => Foundation.UpdatePriority.Input;
    
    private void OnEnable() => UpdateManager.Instance.Register(this);

    private void OnDisable() => UpdateManager.Instance?.Unregister(this);

    public void Tick(float dt)
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Helpers.Input.EnablePlayerInput();
            
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            GameStateManager.RunState.AddCurrency(5);
        }
    }
}
