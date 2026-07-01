using Core;
using Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;

public class Cheats : MonoBehaviour, IUpdatable
{
    public int UpdatePriority => Foundation.UpdatePriority.Input;
    
    private void OnEnable() => UpdateManager.Instance.Register(this);

    private void OnDisable() => UpdateManager.Instance?.Unregister(this);

    public void Tick(float dt)
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            GameStateManager.RunState.AddCurrency(5);
        }
    }
}
