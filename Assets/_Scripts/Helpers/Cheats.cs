using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;

public class Cheats : MonoBehaviour
{
    [SerializeField] private List<Transform> _enemyShooting = new();
    [SerializeField] private List<Transform> _enemyIdle = new();
    [SerializeField] private DummyEnemy _enemyPrefab;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "Prototype")
            return;
        
        foreach (Transform t in _enemyShooting)
        {
            if (t.childCount > 0)
                if (t.GetChild(0).TryGetComponent<DummyEnemy>(out var e))
                {
                    e.Reset();
                    continue;
                }
            var enemy = Instantiate(_enemyPrefab, t.position, t.rotation, t);
            enemy.CanAttack = true;
        }

        foreach (Transform t in _enemyIdle)
        {
            if (t.childCount > 0)
                if (t.GetChild(0).TryGetComponent<DummyEnemy>(out var e))
                {
                    e.Reset();
                    continue;
                }
            Instantiate(_enemyPrefab, t.position, t.rotation, t);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RespawnEnemies();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (SceneManager.GetActiveScene().name == "Prototype")
                return;
            SceneManager.LoadScene("Prototype");
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (SceneManager.GetActiveScene().name == "Room Testing")
                return;
            SceneManager.LoadScene("Room Testing");
        }
    }

    private void RespawnEnemies()
    {
        if (SceneManager.GetActiveScene().name == "Room Testing")
            SceneManager.LoadScene("Room Testing");
        
        if (SceneManager.GetActiveScene().name != "Prototype")
            return;
        
        foreach (Transform t in _enemyShooting)
        {
            if (t.childCount > 0)
                if (t.GetChild(0).TryGetComponent<DummyEnemy>(out var e))
                {
                    e.Reset();
                    continue;
                }
            var enemy = Instantiate(_enemyPrefab, t.position, t.rotation, t);
            enemy.CanAttack = true;
        }

        foreach (Transform t in _enemyIdle)
        {
            if (t.childCount > 0)
                if (t.GetChild(0).TryGetComponent<DummyEnemy>(out var e))
                {
                    e.Reset();
                    continue;
                }
            Instantiate(_enemyPrefab, t.position, t.rotation, t);
        }
    }
}
