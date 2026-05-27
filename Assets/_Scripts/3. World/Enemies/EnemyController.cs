using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Foundation;
using Random = UnityEngine.Random;

namespace World
{
    [RequireComponent(typeof(BlackboardController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyController : MonoBehaviour, IEnemyUpdate, IPoolable
    {
        #region Identity
        public float interval { get; set; }
        public float timer { get; set; }
        public EnemyType Type { get; set; }
        #endregion

        #region Components
        private Blackboard _blackboard;
        public Blackboard Blackboard => _blackboard;
        private EnemyHealth _enemyHealth;
        private AIBrain _aiBrain;
        #endregion

        [Header("Enemy Data")]
        [SerializeField] private EnemyStats _enemyStats;
        private List<IEnemyComponent> _components = new List<IEnemyComponent>();

        public event Action<EnemyController> OnDeathEvent;

        private int _floorLayerMask;

        public void Awake()
        {
            BlackboardController _bcontroller = GetComponent<BlackboardController>();
            _blackboard = _bcontroller.GetBlackboard();
            
            _floorLayerMask = LayerMask.GetMask("Floor");

            _enemyHealth = GetComponent<EnemyHealth>();
            _aiBrain = GetComponent<AIBrain>();

            _components.Add(_aiBrain);
            _components.Add(_enemyHealth);
            _components.Add(_bcontroller);
        }
        public void Start()
        {
            InitSystems();
        }
        private void InitSystems()
        {
            foreach (var component in _components)
            {
                component.InitComponent(_enemyStats, _blackboard);
            }
        }
        private void RestartSystems()
        {
            foreach(var component in _components)
            {
                component.ResetComponent();
            } 
        }
        public void OnDespawn()
        {
            _enemyHealth.OnDeath -= DeathEvent;
            gameObject.SetActive(false);
            CustomUpdateEnemyManager.Instance?.Unregister(this);
        }
        public void OnSpawn()
        {
            gameObject.SetActive(true);
            RestartSystems();
            _enemyHealth.OnDeath -= DeathEvent;
            _enemyHealth.OnDeath += DeathEvent;
            StartCoroutine(WaitForNavMeshAndBindRoutine());
        }
        private IEnumerator WaitForNavMeshAndBindRoutine()
        {
            yield return new WaitForEndOfFrame();

            int maxAttempts = 10;
            int attempts = 0;
            bool boundSuccessfully = false;

            Vector2 jitter2D = Random.insideUnitCircle * 0.2f;
            Vector3 initialPos = transform.position + new Vector3(jitter2D.x, 0, jitter2D.y);

            while (attempts < maxAttempts && !boundSuccessfully)
            {
                Vector3 searchPos = initialPos;
                
                if (Physics.Raycast(initialPos + Vector3.up * 2f, Vector3.down, out RaycastHit rayHit, 10f, _floorLayerMask))
                {
                    searchPos = rayHit.point;
                }
                if (NavMesh.SamplePosition(searchPos, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;

                    _aiBrain.Agent.enabled = true;
                    _aiBrain.Agent.Warp(hit.position);

                    Debug.Log($"<color=green>SUCCESS:</color> Bound {gameObject.name} to NavMesh on attempt {attempts + 1}. Agent is on NavMesh: {_aiBrain.Agent.isOnNavMesh}");

                    boundSuccessfully = true;
                    CustomUpdateEnemyManager.Instance.Register(this);
                }
                else
                {
                    attempts++;
                    yield return null;
                }
            }

            if (!boundSuccessfully)
            {
                Debug.LogError(
                    $"<color=red>FATAL ERROR:</color> {gameObject.name} could not find the NavMesh after {maxAttempts} frames. " +
                    $"Position: {transform.position}. Ensure the Room's NavMeshSurface has completely finished building before spawning this enemy!");
            }
        }
        public void DeathEvent()
        {
            OnDeathEvent?.Invoke(this);
            OnDeathEvent = null;
        }
        public void Tick()
        {
            _aiBrain.Tick();
            _enemyHealth.Tick();
        }

        // ---- Corutina auxiliar ----
        public bool HasDeathListeners()
        {
            return OnDeathEvent != null && OnDeathEvent.GetInvocationList().Length > 0;
        }
    }
}