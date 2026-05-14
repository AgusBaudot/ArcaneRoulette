using Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cheats : MonoBehaviour, IUpdatable
{
    public int UpdatePriority => Foundation.UpdatePriority.Input;
    
    private readonly string _scene1 = "Working Core Loop";
    private readonly string _scene2 = "Working Hardcore Room";
    
    private void OnEnable() => UpdateManager.Instance.Register(this);

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
    }
}
