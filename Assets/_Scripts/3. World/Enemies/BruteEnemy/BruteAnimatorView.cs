using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(Animator), typeof(BruteAIBrain), typeof(NavMeshAgent))]
    public sealed class BruteAnimatorView : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Animations;

        [Header("Visuals")] 
        [SerializeField] private SpriteRenderer _renderer;

        private Animator _animator;
        private BruteAIBrain _brain;
        private NavMeshAgent _agent;
        private AIState _currentState;

        private readonly int _tIdle = Animator.StringToHash("t_Idle");
        private readonly int _tCharge = Animator.StringToHash("t_Charge");
        private readonly int _tThrust = Animator.StringToHash("t_Thrust");
        private readonly int _tAoEAttack = Animator.StringToHash("t_AoE_Attack");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _brain = GetComponent<BruteAIBrain>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnThrustWindupStarted += HandleThrustWindup;
            _brain.OnThrustStarted += HandleThrust;
            _brain.OnChargeWindupStarted += HandleChargeWindup;
            _brain.OnChargeStarted += HandleCharge;
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnThrustWindupStarted -= HandleThrustWindup;
            _brain.OnThrustStarted -= HandleThrust;
            _brain.OnChargeWindupStarted -= HandleChargeWindup;
            _brain.OnChargeStarted -= HandleCharge;
        }

        public void Tick(float dt)
        {
            if (_renderer == null) return;

            if (_currentState == AIState.Chase && _agent.velocity.sqrMagnitude > 0.01f)
            {
                _renderer.flipX = _agent.velocity.x > 0.01f;
            }
            else if (_currentState == AIState.Attack)
            {
                _renderer.flipX = _brain.CurrentAttackDirection.x > 0.01f;
            }
        }

        private void HandleStateChanged(AIState newState)
        {
            _currentState = newState;
            
            if (newState != AIState.Attack)
            {
                _animator.SetTrigger(_tIdle);
            }
        }

        private void HandleThrustWindup()
        {
            _animator.SetTrigger(_tAoEAttack);
        }

        private void HandleThrust()
        {
            _animator.SetTrigger(_tThrust);
        }

        private void HandleChargeWindup()
        {
            _animator.SetTrigger(_tCharge);
        }

        private void HandleCharge()
        {
            _animator.SetTrigger(_tCharge);
        }
    }
}