using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace World 
{
    [RequireComponent(typeof(BlackboardController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyController : MonoBehaviour, IEnemyUpdate, IPooleable
    {
        public float interval { get; set; }
        public float timer { get; set; }
        public EnemyType Type { get; set; }

        private Blackboard _blackboard;
        public Blackboard Blackboard => _blackboard;
        private EnemyHealth _enemyHealth;
        private AIBrain _aiBrain;

        public event Action<EnemyController> OnDeath;

        public void Awake()
        {
            var controller = GetComponent<BlackboardController>();
            _blackboard = controller.GetBlackboard();
            _enemyHealth = GetComponent<EnemyHealth>();
            _aiBrain = GetComponent<AIBrain>();

            _aiBrain.Init(this);
            _enemyHealth.OnDeath += DeathEvent;
        }
        public void OnDespawn()
        {
            gameObject.SetActive(false);
            CustomUpdateEnemyManager.Instance?.Unregister(this);
        }
        public void OnSpawn()
        {
            gameObject.SetActive(true);
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas)) 
            {
                transform.position = hit.position;
                _aiBrain.Agent.enabled = true;
                _aiBrain.Agent.Warp(transform.position);
                Debug.LogWarning(_aiBrain.Agent.isOnNavMesh);
            }
            CustomUpdateEnemyManager.Instance.Register(this);
        }
        public void OnEnable() 
        {
            gameObject.SetActive(true);
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                _aiBrain.Agent.enabled = true;
                _aiBrain.Agent.Warp(transform.position);
                Debug.LogWarning(_aiBrain.Agent.isOnNavMesh);
            }
            CustomUpdateEnemyManager.Instance.Register(this);
        }
        public void DeathEvent()
        {
            OnDeath?.Invoke(this);
            OnDeath = null;
        }
        public void Tick()
        {
            _aiBrain.Tick();
            _enemyHealth.Tick();
        }
    }
}


