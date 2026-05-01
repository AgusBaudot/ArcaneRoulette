using UnityEngine;
using UnityEngine.SceneManagement;

public class Cheats : MonoBehaviour
{
    private string _scene1 = "Core loop";
    private string _scene2 = "Hardcore room";

    private void Update()
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
