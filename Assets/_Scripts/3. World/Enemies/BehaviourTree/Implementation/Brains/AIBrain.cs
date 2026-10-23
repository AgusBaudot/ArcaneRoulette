using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace World
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(BlackboardController))]
    public abstract class AIBrain : MonoBehaviour, IEnemyComponent, IDebuffReceiver
    {
        public event Action<AIState> OnStateChanged;
        
        public NavMeshAgent Agent => _agent;

        [Header("Components Reference")]
        [SerializeField] protected Animator _animator;
        [SerializeField] protected NavMeshAgent _agent;
        protected LineOfSight _los;

        [Header("Basic AI Data")]
        [SerializeField] protected Blackboard _blackboard;
        [SerializeField] protected BehaviorTree _tree;
        [SerializeField] protected Transform _player;
        [SerializeField] protected string _behaviourTreeName;

        [Header("BB Shared Data")]
        [SerializeField] protected List<Transform> _waypoints;
        protected BlackboardKey hasSeenPlayerKey;
        protected IDebuffReadable _debuffs;
        protected bool _wasInRange;

        [Header("Stats")]
        protected EnemyStats _enemyStats;
        protected AIState _currentState;

        private EnemyHealth _enemyHealth;

        protected float EffectiveAttackSpeed
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.AttackSpeed))
                {
                    return _enemyStats.AttackSpeed * Mathf.Max(0f, 1f + _debuffs.GetDebuffStrength(DebuffType.AttackSpeed));
                }
                return _enemyStats.AttackSpeed;
            }
        }
        protected float EffectiveAttackDamage
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.ATK))
                {
                    return _enemyStats.AttackDamage * Mathf.Max(0f, 1f - _debuffs.GetDebuffStrength(DebuffType.ATK));
                }
                return _enemyStats.AttackDamage;
            }
        }
        protected float EffectiveChaseSpeed
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.Speed))
                {
                    return _enemyStats.ChaseSpeed * Mathf.Max(0f, 1f - _debuffs.GetDebuffStrength(DebuffType.Speed));
                }
                return _enemyStats.ChaseSpeed;
            }
        }

        protected virtual void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _los = new LineOfSight();
            _agent.updateRotation = false;
            _agent.avoidancePriority = Random.Range(0, 100);

            _waypoints ??= new List<Transform>();

            GetPlayer();
            if (_player != null)
                _waypoints.Add(_player);

            _enemyHealth = GetComponent<EnemyHealth>();
            if (_enemyHealth != null)
                _enemyHealth.OnDeath += HandleDeath;
        }

        protected void SetCollidersEnabled(bool isEnabled)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.enabled = isEnabled;
            }
        }

        private void HandleDeath()
        {
            SetState(AIState.Death);

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }

            SetCollidersEnabled(false);
        }

        protected Transform GetPlayer()
        {
            if (_player == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    _player = playerGO.transform;
            }
            return _player;
        }

        public void InitComponent(EnemyStats stats, Blackboard bb)
        {
            _blackboard = bb;
            _enemyStats = stats;
            _agent.stoppingDistance = _enemyStats.AttackRange - 1f;
            _los.Init(transform, _enemyStats.ViewDistance, _enemyStats.ObsMask);
            
            hasSeenPlayerKey = _blackboard.GetOrRegisterKey("hasSeenPlayer"); 
            
            _tree = BuildTree();
        }
        
        public virtual void ResetComponent()
        {
            _debuffs = null;
            _tree?.Reset();
            _wasInRange = false;
            
            _currentState = (AIState)(-1); 

            SetCollidersEnabled(false);

            if (_animator != null)
            {
                _animator.Rebind();
                _animator.Update(0f); 
            }

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.isStopped = false; 
            }
        }
        
        public void Tick()
        {
            if (IsState(AIState.Death))
                return;
            
            _tree?.Process();
        }
        protected abstract BehaviorTree BuildTree();
        protected bool IsState(AIState state) => _currentState == state;

        protected void SetState(AIState newState)
        {
            if (_currentState == newState)
                return;
            
            AIState previousState = _currentState;
            _currentState = newState;

            if (newState == AIState.Spawning || newState == AIState.Death)
            {
                SetCollidersEnabled(false);
            }
            else if (previousState == AIState.Spawning || previousState == (AIState)(-1))
            {
                SetCollidersEnabled(true);
            }

            OnStateChanged?.Invoke(_currentState);
        }

        protected virtual bool IsInLos()
        {
            if (_blackboard.TryGetValue<bool>(hasSeenPlayerKey, out var seen) && seen)
                return true;

            Transform player = GetPlayer();
            if (player == null)
                return false;

            bool hasLOS = _los.CheckRange(player) && _los.CheckView(player);
            _blackboard.SetValue(hasSeenPlayerKey, hasLOS);
            return hasLOS;
        }
        
        protected virtual bool IsInStableDistance(Transform target)
        {
            if (target == null) 
                return false;

            float distance = Vector3.Distance(transform.position, target.position);
            bool result;
            if (_wasInRange)
                result = distance <= _enemyStats.ExitAttackRange;
            else
                result = distance <= _enemyStats.AttackRange;
            _wasInRange = result;
            return result;
        }

        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            if (_enemyStats == null) return;

            Color Color1 = Color.red;
            Color1.a = 0.5f;
            Gizmos.color = Color1;
            Gizmos.DrawWireSphere(transform.position, _enemyStats.AttackRange);

            Color Color2 = Color.blue;
            Color2.a = 0.5f;
            Gizmos.color = Color2;
            Gizmos.DrawWireSphere(transform.position, _enemyStats.ViewDistance);
        }
        #endregion
    }
}