using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI 
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private SceneController _sceneController;
        public void StartGame()
        {
            _sceneController.LoadScene("Room Testing");
        }

        public void QuitGame()
        {
            Debug.Log("Quit");
            Application.Quit();
        }
    }
}

