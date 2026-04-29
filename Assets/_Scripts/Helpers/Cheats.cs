using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;

public class Cheats : MonoBehaviour
{
    [SerializeField] private List<Transform> _enemyShooting = new();
    [SerializeField] private List<Transform> _enemyIdle = new();
    [SerializeField] private DummyEnemy _enemyPrefab;
    
    private string _scene1 = "Core loop";
    private string _scene2 = "Hardcore room";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RespawnEnemies();
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
    }

    private void RespawnEnemies()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
