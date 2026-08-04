using System;
using System.Collections;
using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace World
{
    [RequireComponent(typeof(BlackboardController))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(AIBrain))]
    public class EnemyController : MonoBehaviour, IEnemyUpdate, IPoolable, IAlly
    {
        #region Parameters
        public float interval { get; set; }
        public float timer { get; set; }
        public EnemyType Type { get; set; }
        public IHealable Healable => _healable;
        public bool IsBeingHealed { get; set; }
        public Transform Transform => transform;
        public Blackboard Blackboard => _blackboard;
        #endregion

        #region Components
        private Blackboard _blackboard;
        private EnemyHealth _enemyHealth;
        private AIBrain _aiBrain;
        private IHealable _healable;
        private Rigidbody _rb;
        private List<IEnemyComponent> _components = new List<IEnemyComponent>();
        #endregion

        [Header("Enemy Data")]
        [SerializeField] private EnemyStats _enemyStats;
        [Header("References")]
        [SerializeField] private SpriteRenderer _elementalFeedback;
        [SerializeField] private Sprite _fireElement;
        [SerializeField] private Sprite _earthElement;
        [SerializeField] private Sprite _waterElement;
        [SerializeField] private Sprite _thunderElement;
        
        public event Action<EnemyController> OnDeathEvent;
        private int _floorLayerMask;
        private bool _isInitialized;

        public void Awake()
        {
            BlackboardController _bcontroller = GetComponent<BlackboardController>();
            _blackboard = _bcontroller.GetBlackboard();
            _floorLayerMask = LayerMask.GetMask("Floor");
            _enemyHealth = GetComponent<EnemyHealth>();
            _healable = GetComponent<IHealable>();
            _aiBrain = GetComponent<AIBrain>();
            _rb = GetComponent<Rigidbody>();
            _components.Add(_aiBrain);
            _components.Add(_enemyHealth);
            _components.Add(_bcontroller);
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
            OnDeathEvent = null; 
    
            if (_aiBrain.Agent != null && _aiBrain.Agent.isActiveAndEnabled)
            {
                _aiBrain.Agent.enabled = false; 
            }

            gameObject.SetActive(false);
            CustomUpdateEnemyManager.Instance?.Unregister(this);
        }
        public void OnSpawn()
        {
            gameObject.SetActive(true);
    
            if (!_isInitialized)
            {
                switch (_enemyStats.ElementType)
                {
                    case ElementType.Fire: _elementalFeedback.sprite = _fireElement; break;
                    case ElementType.Water: _elementalFeedback.sprite = _waterElement; break;
                    case ElementType.Earth: _elementalFeedback.sprite = _earthElement; break;
                    case ElementType.Electric: _elementalFeedback.sprite = _thunderElement; break;
                }
        
                InitSystems();
                _isInitialized = true;
            }

            RestartSystems();
    
            _enemyHealth.OnDeath -= DeathEvent;
            _enemyHealth.OnDeath += DeathEvent;
    
            BindToNavMesh(); 
    
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            IsBeingHealed = false;
        }

        public void BindToNavMesh()
        {
            StartCoroutine(WaitForNavMeshAndBindRoutine());
        }
        
        private IEnumerator WaitForNavMeshAndBindRoutine()
        {
            yield return new WaitForEndOfFrame();

            Vector2 jitter = Random.insideUnitCircle * 0.3f;
            Vector3 searchPos = transform.position + new Vector3(jitter.x, 0, jitter.y);

            if (NavMesh.SamplePosition(searchPos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                _aiBrain.Agent.enabled = true;
                _aiBrain.Agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError($"<color=red>FATAL ERROR:</color> {gameObject.name} failed to find NavMesh at {searchPos}. " + "Check your NavMesh surface and ensure it covers the spawn colliders!");      
            }
            while (CustomUpdateEnemyManager.Instance == null) 
            {
                yield return null;
            }
            Debug.Log($"<color=green>SUCCESS:</color> {gameObject.name} Succesfuly register to CustomUpdateEnemyManager.");
            CustomUpdateEnemyManager.Instance.Register(this);
        }
        
        public void DeathEvent()
        {
            OnDeathEvent?.Invoke(this);
        }
        public void Tick()
        {
            _aiBrain.Tick();
            _enemyHealth.Tick();
        }
    }
}