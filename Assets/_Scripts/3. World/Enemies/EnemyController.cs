using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World;

[RequireComponent(typeof(BlackboardController))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour, IEnemyUpdate, IPooleable
{
    public float interval { get; set; }
    public float timer { get; set; }

    private Blackboard _blackboard;
    private EnemyHealth _enemyHealth;
    private AIBrain _aiBrain;

    public void Awake()
    {
        var controller = GetComponent<BlackboardController>();
        _blackboard = controller.GetBlackboard();
        _enemyHealth = GetComponent<EnemyHealth>();
        _aiBrain = GetComponent<AIBrain>();
    }
    public void OnDespawn()
    {
        gameObject.SetActive(false);
        CustomUpdateEnemyManager.Instance?.Unregister(this);
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
        CustomUpdateEnemyManager.Instance.Register(this);
    }

    public void Tick()
    {
        _aiBrain.Tick();
        //_enemyHealth.Tick();
    }
}
