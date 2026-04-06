using System.Collections.Generic;
using UnityEngine;
using World;

public class Cheats : MonoBehaviour
{
    [SerializeField] private List<Transform> _enemyShooting = new();
    [SerializeField] private List<Transform> _enemyIdle = new();
    [SerializeField] private DummyEnemy _enemyPrefab;

    private void Start()
    {
        RespawnEnemies();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RespawnEnemies();
        }
    }

    private void RespawnEnemies()
    {
        foreach (Transform t in _enemyShooting)
        {
            if (t.childCount > 0) continue;
            var enemy = Instantiate(_enemyPrefab, t.position, t.rotation, t);
            enemy.CanAttack = true;
        }

        foreach (Transform t in _enemyIdle)
        {
            if (t.childCount > 0) continue;
            Instantiate(_enemyPrefab, t.position, t.rotation, t);
        }
    }
}
